using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Nominations.Dtos;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
 

namespace MOD.Training.Training.Nominations;

[Authorize(TrainingPermissions.Nomination.Default)]
public class NominationAppService(
    IRepository<Nomination, Guid> repository,
    IRepository<NominationApproval, Guid> approvalRepository,
    IRepository<CourseSession, Guid> sessionRepository,
    EmployeeResolver employeeResolver,
    CourseNameResolver courseNameResolver,
    NominationConditionValidator conditionValidator,
    NominationToDtoMapper toDtoMapper,
    NominationApprovalToDtoMapper approvalToDtoMapper)
    : ApplicationService, INominationAppService
{
    public async Task<NominationDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        var dto = toDtoMapper.Map(entity);
        await EnrichDtoAsync(dto, entity);
        return dto;
    }

    public async Task<PagedResultDto<NominationDto>> GetListAsync(NominationGetListInput input)
    {
        var queryable = await repository.GetQueryableAsync();

        if (input.SessionId.HasValue)
            queryable = queryable.Where(x => x.SessionId == input.SessionId.Value);
        if (input.Status.HasValue)
            queryable = queryable.Where(x => x.Status == input.Status.Value);
        if (input.EmployeeId.HasValue)
            queryable = queryable.Where(x => x.EmployeeId == input.EmployeeId.Value);

        var totalCount = await AsyncExecuter.CountAsync(queryable);
        queryable = queryable.OrderByDescending(x => x.NominatedAt);
        queryable = queryable.PageBy(input);

        var entities = await AsyncExecuter.ToListAsync(queryable);

        // Batch resolve employee names
        var employeeIds = entities.Select(x => x.EmployeeId).Distinct().ToList();
        var nominatorUserIds = entities.Select(x => x.NominatedById).Distinct().ToList();
        var sessionIds = entities.Select(x => x.SessionId).Distinct().ToList();

        var employees = await employeeResolver.BatchResolveByIdsAsync(employeeIds);
        var nominators = await employeeResolver.BatchResolveByUserIdsAsync(nominatorUserIds);

        // Resolve session → course names
        var sessionCourseMap = new Dictionary<Guid, (string code, string nameAr)>();
        foreach (var sid in sessionIds.Where(s => s.HasValue).Select(s => s!.Value).Distinct())
        {
            var session = await sessionRepository.FindAsync(sid);
            if (session != null)
            {
                var course = await courseNameResolver.ResolveAsync(session.CourseId);
                sessionCourseMap[sid] = (session.SessionCode, course?.NameAr ?? "");
            }
        }

        var dtos = entities.Select(e =>
        {
            var dto = toDtoMapper.Map(e);

            if (employees.TryGetValue(e.EmployeeId, out var emp))
            {
                dto.EmployeeName = emp.FullNameAr;
            }

            if (nominators.TryGetValue(e.NominatedById, out var nominator))
            {
                dto.NominatedByName = nominator.FullNameAr;
            }

            if (e.SessionId.HasValue && sessionCourseMap.TryGetValue(e.SessionId.Value, out var sessionInfo))
            {
                dto.SessionCode = sessionInfo.code;
                dto.CourseName = sessionInfo.nameAr;
            }

            return dto;
        }).ToList();

        return new PagedResultDto<NominationDto>(totalCount, dtos);
    }

    // CreateBatchAsync — removed (moved to TrainingPlanItemAppService.CreateAsync)
    [Obsolete("Moved to TrainingPlanItemAppService.CreateAsync")]
    [Authorize(TrainingPermissions.Nomination.Create)]
    public Task<List<NominationDto>> CreateBatchAsync(CreateNominationDto input)
    {
        throw new Volo.Abp.BusinessException("Training:Nomination:UsePlanItemCreate");
    }

    public async Task ApproveAsync(Guid id, ApproveRejectNominationDto input)
    {
        var nomination = await repository.GetAsync(id);

        var approvalsQueryable = await approvalRepository.GetQueryableAsync();
        var pendingApproval = await AsyncExecuter.FirstOrDefaultAsync(
            approvalsQueryable
                .Where(x => x.NominationId == id && x.Status == ApprovalStatus.Pending)
                .OrderBy(x => x.ApprovalLevel));

        if (pendingApproval == null) return;

        if (pendingApproval.ApprovalLevel == 2)
            await AuthorizationService.CheckAsync(TrainingPermissions.Nomination.ApproveUGM);
        else if (pendingApproval.ApprovalLevel == 3)
            await AuthorizationService.CheckAsync(TrainingPermissions.Nomination.ApproveTD);

        pendingApproval.ApprovedById = CurrentUser.Id;
        pendingApproval.Status = ApprovalStatus.Approved;
        pendingApproval.ActionDate = DateTime.Now;
        pendingApproval.Notes = input.Notes;
        await approvalRepository.UpdateAsync(pendingApproval, autoSave: true);

        nomination.Status = pendingApproval.ApprovalLevel switch
        {
            2 => NominationStatus.UGMApproved,
            3 => NominationStatus.TDApproved,
            _ => nomination.Status
        };

        if (pendingApproval.ApprovalLevel == 3)
            nomination.ApprovedAt = DateTime.Now;

        await repository.UpdateAsync(nomination, autoSave: true);
    }

    public async Task RejectAsync(Guid id, ApproveRejectNominationDto input)
    {
        var nomination = await repository.GetAsync(id);

        var approvalsQueryable = await approvalRepository.GetQueryableAsync();
        var pendingApproval = await AsyncExecuter.FirstOrDefaultAsync(
            approvalsQueryable
                .Where(x => x.NominationId == id && x.Status == ApprovalStatus.Pending)
                .OrderBy(x => x.ApprovalLevel));

        if (pendingApproval == null) return;

        pendingApproval.ApprovedById = CurrentUser.Id;
        pendingApproval.Status = ApprovalStatus.Rejected;
        pendingApproval.ActionDate = DateTime.Now;
        pendingApproval.Notes = input.Notes;
        await approvalRepository.UpdateAsync(pendingApproval, autoSave: true);

        nomination.Status = NominationStatus.Rejected;
        await repository.UpdateAsync(nomination, autoSave: true);

        // Restore available seat if session is assigned
        if (nomination.SessionId.HasValue)
        {
            var session = await sessionRepository.GetAsync(nomination.SessionId.Value);
            session.AvailableSeats++;
            await sessionRepository.UpdateAsync(session, autoSave: true);
        }
    }

    public async Task<List<NominationApprovalDto>> GetApprovalChainAsync(Guid nominationId)
    {
        var queryable = await approvalRepository.GetQueryableAsync();
        var approvals = await AsyncExecuter.ToListAsync(
            queryable.Where(x => x.NominationId == nominationId)
                .OrderBy(x => x.ApprovalLevel));

        // Resolve approver names
        var approverUserIds = approvals
            .Where(a => a.ApprovedById.HasValue)
            .Select(a => a.ApprovedById!.Value)
            .Distinct().ToList();
        var approvers = await employeeResolver.BatchResolveByUserIdsAsync(approverUserIds);

        return approvals.Select(a =>
        {
            var dto = approvalToDtoMapper.Map(a);
            dto.ApprovalLevelName = a.ApprovalLevel switch
            {
                1 => "مدير التدريب بالوحدة (UTM)",
                2 => "المدير العام للوحدة (UGM)",
                3 => "مدير التدريب (TD)",
                _ => $"المستوى {a.ApprovalLevel}"
            };

            if (a.ApprovedById.HasValue && approvers.TryGetValue(a.ApprovedById.Value, out var approver))
            {
                dto.ApprovedByName = approver.FullNameAr;
            }

            return dto;
        }).ToList();
    }

    private async Task EnrichDtoAsync(NominationDto dto, Nomination entity)
    {
        var emp = await employeeResolver.GetByIdAsync(entity.EmployeeId);
        if (emp != null) dto.EmployeeName = emp.FullNameAr;

        var nominator = await employeeResolver.GetByUserIdAsync(entity.NominatedById);
        if (nominator != null) dto.NominatedByName = nominator.FullNameAr;

        if (entity.SessionId.HasValue)
        {
            var session = await sessionRepository.FindAsync(entity.SessionId.Value);
            if (session != null) dto.SessionCode = session.SessionCode;
        }
    }
}
