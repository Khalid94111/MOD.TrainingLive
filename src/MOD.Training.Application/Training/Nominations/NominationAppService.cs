using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Enums;
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
    NominationToDtoMapper toDtoMapper,
    NominationApprovalToDtoMapper approvalToDtoMapper)
    : ApplicationService, INominationAppService
{
    public async Task<NominationDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        return toDtoMapper.Map(entity);
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
        var dtos = entities.Select(e => toDtoMapper.Map(e)).ToList();

        // TODO: batch resolve EmployeeName, SessionCode, CourseName via HR + session lookups

        return new PagedResultDto<NominationDto>(totalCount, dtos);
    }

    // UTM batch nominates (MOD-17)
    [Authorize(TrainingPermissions.Nomination.Create)]
    public async Task<List<NominationDto>> CreateBatchAsync(CreateNominationDto input)
    {
        var session = await sessionRepository.GetAsync(input.SessionId);

        // Validate available seats
        if (session.AvailableSeats < input.EmployeeIds.Count)
            throw new Volo.Abp.BusinessException("Training:Nomination:SessionFull");

        var results = new List<NominationDto>();

        foreach (var employeeId in input.EmployeeIds)
        {
            // Check duplicate
            var existsQueryable = await repository.GetQueryableAsync();
            var alreadyNominated = await AsyncExecuter.AnyAsync(
                existsQueryable.Where(x => x.SessionId == input.SessionId && x.EmployeeId == employeeId));
            if (alreadyNominated)
                throw new Volo.Abp.BusinessException("Training:Nomination:AlreadyNominated");

            // TODO: validate 9 conditions against employee HR data
            // await ValidateConditionsAsync(input.SessionId, employeeId);

            var nomination = new Nomination(
                GuidGenerator.Create(),
                input.SessionId,
                employeeId,
                CurrentUser.Id!.Value);

            // Auto-approve level 1 (UTM) since UTM is the creator
            nomination.Status = NominationStatus.UTMApproved;

            await repository.InsertAsync(nomination, autoSave: true);

            // Create 3-level approval chain
            // Level 1: UTM — auto-approved
            var utmApproval = new NominationApproval(
                GuidGenerator.Create(), nomination.Id, 1);
            utmApproval.ApprovedById = CurrentUser.Id;
            utmApproval.Status = ApprovalStatus.Approved;
            utmApproval.ActionDate = DateTime.Now;
            await approvalRepository.InsertAsync(utmApproval, autoSave: true);

            // Level 2: UGM — pending
            await approvalRepository.InsertAsync(
                new NominationApproval(GuidGenerator.Create(), nomination.Id, 2),
                autoSave: true);

            // Level 3: TD — pending
            await approvalRepository.InsertAsync(
                new NominationApproval(GuidGenerator.Create(), nomination.Id, 3),
                autoSave: true);

            // Decrement available seats
            session.AvailableSeats--;

            results.Add(toDtoMapper.Map(nomination));
        }

        await sessionRepository.UpdateAsync(session, autoSave: true);

        return results;
    }

    // UGM or TD approves
    public async Task ApproveAsync(Guid id, ApproveRejectNominationDto input)
    {
        var nomination = await repository.GetAsync(id);

        // Find the next pending approval level
        var approvalsQueryable = await approvalRepository.GetQueryableAsync();
        var pendingApproval = await AsyncExecuter.FirstOrDefaultAsync(
            approvalsQueryable
                .Where(x => x.NominationId == id && x.Status == ApprovalStatus.Pending)
                .OrderBy(x => x.ApprovalLevel));

        if (pendingApproval == null)
            return; // Already fully approved

        // Authorize based on level
        if (pendingApproval.ApprovalLevel == 2)
            await AuthorizationService.CheckAsync(TrainingPermissions.Nomination.ApproveUGM);
        else if (pendingApproval.ApprovalLevel == 3)
            await AuthorizationService.CheckAsync(TrainingPermissions.Nomination.ApproveTD);

        pendingApproval.ApprovedById = CurrentUser.Id;
        pendingApproval.Status = ApprovalStatus.Approved;
        pendingApproval.ActionDate = DateTime.Now;
        pendingApproval.Notes = input.Notes;
        await approvalRepository.UpdateAsync(pendingApproval, autoSave: true);

        // Update nomination status
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

        // Restore available seat
        var session = await sessionRepository.GetAsync(nomination.SessionId);
        session.AvailableSeats++;
        await sessionRepository.UpdateAsync(session, autoSave: true);
    }

    public async Task<List<NominationApprovalDto>> GetApprovalChainAsync(Guid nominationId)
    {
        var queryable = await approvalRepository.GetQueryableAsync();
        var approvals = await AsyncExecuter.ToListAsync(
            queryable.Where(x => x.NominationId == nominationId)
                .OrderBy(x => x.ApprovalLevel));

        return approvals.Select(a =>
        {
            var dto = approvalToDtoMapper.Map(a);
            dto.ApprovalLevelName = a.ApprovalLevel switch
            {
                1 => "UTM",
                2 => "UGM",
                3 => "TD",
                _ => $"Level {a.ApprovalLevel}"
            };
            return dto;
        }).ToList();
    }
}
