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
using MOD.Training.Training.Plans.Dtos;
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
    PlanItemRankBreakdownManager rankBreakdownManager,
    PlanItemUnitScope unitScope,
    IPlanNoteAppService planNoteAppService,
    NominationToDtoMapper toDtoMapper,
    NominationApprovalToDtoMapper approvalToDtoMapper)
    : ApplicationService, INominationAppService
{
    public async Task<NominationDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessPlanItemAsync(entity.PlanItemId);
        var dto = toDtoMapper.Map(entity);
        await EnrichDtoAsync(dto, entity);
        return dto;
    }

    public async Task<PagedResultDto<NominationDto>> GetListAsync(NominationGetListInput input)
    {
        var queryable = await repository.GetQueryableAsync();

        // Scope to the current user's unit via the parent plan item (UTM/UGM only).
        if (await unitScope.IsCurrentUserUnitScopedAsync())
        {
            var currentUnitId = await unitScope.GetCurrentUserUnitIdAsync();
            queryable = currentUnitId.HasValue
                ? queryable.Where(x => x.PlanItem!.UnitId == currentUnitId.Value)
                : queryable.Where(_ => false);
        }

        if (input.PlanItemId.HasValue)
            queryable = queryable.Where(x => x.PlanItemId == input.PlanItemId.Value);
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
        var sessionIds = entities.Where(x => x.SessionId.HasValue).Select(x => x.SessionId!.Value).Distinct().ToList();

        var employees = await employeeResolver.BatchResolveByIdsAsync(employeeIds);
        var nominators = await employeeResolver.BatchResolveByUserIdsAsync(nominatorUserIds);

        // Resolve session → course names.
        // Phase 4C-α (v4.10.0): CourseSession.CourseId removed, replaced by TenantCourseId.
        // The Phase 3 CourseNameResolver still expects a Courses.Id, so the SessionCode
        // and course-name fields are left empty for the new shape — step 5 of 4C-α will
        // project a session display label via TenantCourse join.
        var sessionCourseMap = new Dictionary<Guid, (string code, string nameAr)>();
        foreach (var sid in sessionIds)
        {
            var session = await sessionRepository.FindAsync(sid);
            if (session != null)
            {
                sessionCourseMap[sid] = (string.Empty, string.Empty);
            }
        }

        var dtos = entities.Select(e =>
        {
            var dto = toDtoMapper.Map(e);

            if (employees.TryGetValue(e.EmployeeId, out var emp))
                dto.EmployeeName = emp.FullNameAr;

            if (nominators.TryGetValue(e.NominatedById, out var nominator))
                dto.NominatedByName = nominator.FullNameAr;

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
        await unitScope.EnsureCanAccessPlanItemAsync(nomination.PlanItemId);

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
        await unitScope.EnsureCanAccessPlanItemAsync(nomination.PlanItemId);

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

        // Phase 4C-α (v4.10.0): CourseSession no longer tracks AvailableSeats — sessions
        // have a fixed nominee snapshot taken at creation (Q-D, locked after creation).
        // Rejection at the plan-item level no longer adjusts session seat counts.
    }

    public async Task<List<NominationApprovalDto>> GetApprovalChainAsync(Guid nominationId)
    {
        var nomination = await repository.GetAsync(nominationId);
        await unitScope.EnsureCanAccessPlanItemAsync(nomination.PlanItemId);

        var queryable = await approvalRepository.GetQueryableAsync();
        var approvals = await AsyncExecuter.ToListAsync(
            queryable.Where(x => x.NominationId == nominationId)
                .OrderBy(x => x.ApprovalLevel));

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
                dto.ApprovedByName = approver.FullNameAr;

            return dto;
        }).ToList();
    }

    [Authorize(TrainingPermissions.Nomination.Return)]
    public async Task ReturnAsync(Guid id, ReturnReasonDto input)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessPlanItemAsync(entity.PlanItemId);

        var noteDto = await planNoteAppService.CreateAsync(new CreatePlanNoteDto
        {
            EntityType = PlanNoteEntityType.Nomination,
            EntityId = id,
            Note = input.Reason,
            IsReturnReason = true
        });

        entity.IsReturned = true;
        entity.LastReturnNoteId = noteDto.Id;
        entity.Status = NominationStatus.Returned;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    [Authorize(TrainingPermissions.Nomination.Replace)]
    public async Task<NominationDto> ReplaceAsync(Guid id, ReplaceNominationDto input)
    {
        var oldNom = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessPlanItemAsync(oldNom.PlanItemId);
        if (!oldNom.IsReturned)
            throw new Volo.Abp.BusinessException("Training:Nomination:NotReturned");

        // Validate new employee against conditions
        var results = await conditionValidator.ValidateByPlanItemAsync(oldNom.PlanItemId, input.NewEmployeeId);
        var failed = results.Where(r => !r.Passed).ToList();
        if (failed.Any())
        {
            var details = string.Join(" | ", failed.Select(f => $"{f.ConditionTypeAr}: {f.Details}"));
            throw new Volo.Abp.BusinessException("Training:Nomination:ConditionFailed")
                .WithData("Details", details);
        }

        // Mark old as rejected
        oldNom.Status = NominationStatus.Rejected;
        await repository.UpdateAsync(oldNom, autoSave: true);

        // Create new nomination
        var newNom = new Nomination(
            GuidGenerator.Create(),
            oldNom.PlanItemId,
            input.NewEmployeeId,
            CurrentUser.Id!.Value);
        await repository.InsertAsync(newNom, autoSave: true);

        // Refresh rank breakdown
        await rankBreakdownManager.RefreshForNomineeChangeAsync(oldNom.PlanItemId);

        var dto = toDtoMapper.Map(newNom);
        await EnrichDtoAsync(dto, newNom);
        return dto;
    }

    private async Task EnrichDtoAsync(NominationDto dto, Nomination entity)
    {
        var emp = await employeeResolver.GetByIdAsync(entity.EmployeeId);
        if (emp != null) dto.EmployeeName = emp.FullNameAr;

        var nominator = await employeeResolver.GetByUserIdAsync(entity.NominatedById);
        if (nominator != null) dto.NominatedByName = nominator.FullNameAr;

        // Phase 4C-α (v4.10.0): CourseSession no longer carries a SessionCode column.
        // Step 5 of 4C-α will project a session display label via TenantCourse join.
        if (entity.SessionId.HasValue)
        {
            var session = await sessionRepository.FindAsync(entity.SessionId.Value);
            if (session != null) dto.SessionCode = string.Empty;
        }
    }
}
