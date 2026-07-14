using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Nominations;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans.Dtos;
using MOD.Training.Training.TenantCourses;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace MOD.Training.Training.Plans;

[Authorize(TrainingPermissions.TrainingPlanItem.Default)]
public class TrainingPlanItemAppService(
    IRepository<TrainingPlanItem, Guid> repository,
    IRepository<TrainingPlan, Guid> planRepository,
    IRepository<Nomination, Guid> nominationRepository,
    IOrganizationUnitRepository orgUnitRepository,
    CourseNameResolver courseNameResolver,
    EmployeeResolver employeeResolver,
    PlanItemCostCalculator costCalculator,
    PlanItemRankBreakdownManager rankBreakdownManager,
    PlanItemUnitScope unitScope,
    IPlanNoteAppService planNoteAppService,
    TrainingPlanItemToDtoMapper toDtoMapper)
    : ApplicationService, ITrainingPlanItemAppService
{
    public async Task<TrainingPlanItemDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessAsync(entity);
        var dto = toDtoMapper.Map(entity);
        await EnrichSingleDtoAsync(dto, entity);
        return dto;
    }

    public async Task<PagedResultDto<TrainingPlanItemDto>> GetListAsync(TrainingPlanItemGetListInput input)
    {
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(x => x.PlanId == input.PlanId);
        queryable = await unitScope.ApplyScopeAsync(queryable);

        if (input.CourseType.HasValue)
            queryable = queryable.Where(x => x.CourseType == input.CourseType.Value);
        if (input.PreferredQuarter.HasValue)
            queryable = queryable.Where(x => x.PreferredQuarter == input.PreferredQuarter.Value);
        if (input.UnitId.HasValue)
            queryable = queryable.Where(x => x.UnitId == input.UnitId.Value);

        var totalCount = await AsyncExecuter.CountAsync(queryable);
        queryable = queryable.OrderBy(x => x.Priority);
        queryable = queryable.PageBy(input);

        var entities = await AsyncExecuter.ToListAsync(queryable);

        // Batch resolve all lookups
        var tcIds = entities.Select(x => x.TenantCourseId).Distinct().ToList();
        var submitterUserIds = entities.Select(x => x.SubmittedById).Distinct().ToList();
        var unitIds = entities.Where(x => x.UnitId.HasValue).Select(x => x.UnitId!.Value).Distinct().ToList();
        var planItemIds = entities.Select(x => x.Id).ToList();

        var courseNames = await courseNameResolver.BatchResolveAsync(tcIds);
        var employees = await employeeResolver.BatchResolveByUserIdsAsync(submitterUserIds);
        var costs = await costCalculator.BatchGetEstimatedCostsAsync(planItemIds);

        // Batch load nominee counts
        var nomQ = await nominationRepository.GetQueryableAsync();
        var nomCounts = await AsyncExecuter.ToListAsync(
            nomQ.Where(x => planItemIds.Contains(x.PlanItemId) && !x.IsReturned)
                .GroupBy(x => x.PlanItemId)
                .Select(g => new { PlanItemId = g.Key, Count = g.Count() }));
        var nomCountMap = nomCounts.ToDictionary(x => x.PlanItemId, x => x.Count);

        // Batch load OrgUnit names
        var unitLookup = new Dictionary<Guid, string>();
        foreach (var uid in unitIds)
        {
            var ou = await orgUnitRepository.FindAsync(uid);
            if (ou != null) unitLookup[uid] = ou.DisplayName;
        }

        var dtos = entities.Select(e =>
        {
            var dto = toDtoMapper.Map(e);

            if (courseNames.TryGetValue(e.TenantCourseId, out var cn))
            {
                dto.TenantCourseNameAr = cn.NameAr;
                dto.TenantCourseNameEn = cn.NameEn;
            }

            if (employees.TryGetValue(e.SubmittedById, out var emp))
            {
                dto.SubmittedByName = emp.FullNameAr;
                dto.SubmittedByRank = emp.Rank?.NameAr;
            }

            if (e.UnitId.HasValue && unitLookup.TryGetValue(e.UnitId.Value, out var unitName))
                dto.UnitName = unitName;

            dto.EstimatedCost = costs.GetValueOrDefault(e.Id, 0);
            dto.NomineesCount = nomCountMap.GetValueOrDefault(e.Id, 0);

            return dto;
        }).ToList();

        return new PagedResultDto<TrainingPlanItemDto>(totalCount, dtos);
    }

    [Authorize(TrainingPermissions.TrainingPlanItem.Create)]
    public async Task<TrainingPlanItemDto> CreateAsync(CreateUpdateTrainingPlanItemDto input)
    {
        var plan = await planRepository.GetAsync(input.PlanId);
        if (plan.Status != PlanStatus.Open && plan.Status != PlanStatus.ReturnedToCreator)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:WindowNotOpen");

        // Unit-scoped callers (UTM/UGM) always get their own unit regardless of DTO value.
        // Non-scoped callers (Staff/TD/TH) may specify input.UnitId; fall back to their
        // Employee.MainUnitId when omitted.
        var isUnitScoped = await unitScope.IsCurrentUserUnitScopedAsync();
        var resolvedUnitId = isUnitScoped
            ? await employeeResolver.GetCurrentUserUnitIdAsync()
            : (input.UnitId ?? await employeeResolver.GetCurrentUserUnitIdAsync());

        var entity = new TrainingPlanItem(
            GuidGenerator.Create(),
            input.PlanId,
            input.TenantCourseId,
            input.CourseType,
            input.PreferredQuarter,
            input.Priority,
            input.Justification,
            CurrentUser.Id!.Value)
        {
            UnitId = resolvedUnitId,
            DescriptionAr = input.DescriptionAr,
            DescriptionEn = input.DescriptionEn,
            ObjectivesAr = input.ObjectivesAr,
            ObjectivesEn = input.ObjectivesEn,
            DurationYears = input.DurationYears,
            DurationMonths = input.DurationMonths,
            DurationDays = input.DurationDays,
            EstimatedDateFrom = input.EstimatedDateFrom,
            EstimatedDateTo = input.EstimatedDateTo,
            FundingSource = input.FundingSource,
        };

        await repository.InsertAsync(entity, autoSave: true);

        if (input.NomineeEmployeeIds.Count == 0)
            throw new Volo.Abp.BusinessException("Training:TrainingPlanItem:AtLeastOneNomineeRequired");

        foreach (var employeeId in input.NomineeEmployeeIds.Distinct())
        {
            await nominationRepository.InsertAsync(
                new Nomination(GuidGenerator.Create(), entity.Id, employeeId, CurrentUser.Id!.Value),
                autoSave: true);
        }

        var dto = toDtoMapper.Map(entity);
        await EnrichSingleDtoAsync(dto, entity);
        return dto;
    }

    [Authorize(TrainingPermissions.TrainingPlanItem.Update)]
    public async Task<TrainingPlanItemDto> UpdateAsync(Guid id, CreateUpdateTrainingPlanItemDto input)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessAsync(entity);
        var plan = await planRepository.GetAsync(entity.PlanId);

        var canEdit = plan.Status == PlanStatus.Open ||
                      plan.Status == PlanStatus.Draft ||
                      (plan.Status == PlanStatus.ReturnedToCreator) ||
                      (plan.Status == PlanStatus.UnderReview && entity.IsReturned);
        if (!canEdit)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:NotEditable");

        var oldDays = entity.DurationDays;

        entity.TenantCourseId = input.TenantCourseId;
        entity.CourseType = input.CourseType;
        entity.PreferredQuarter = input.PreferredQuarter;
        entity.Priority = input.Priority;
        entity.Justification = input.Justification;
        entity.DescriptionAr = input.DescriptionAr;
        entity.DescriptionEn = input.DescriptionEn;
        entity.ObjectivesAr = input.ObjectivesAr;
        entity.ObjectivesEn = input.ObjectivesEn;
        entity.DurationYears = input.DurationYears;
        entity.DurationMonths = input.DurationMonths;
        entity.DurationDays = input.DurationDays;
        entity.EstimatedDateFrom = input.EstimatedDateFrom;
        entity.EstimatedDateTo = input.EstimatedDateTo;
        entity.FundingSource = input.FundingSource;
        entity.IsReturned = false; // saving clears the return flag

        await repository.UpdateAsync(entity, autoSave: true);

        // CHG-07 cascade — DurationDays changed
        if (oldDays != input.DurationDays)
            await rankBreakdownManager.RefreshForDaysChangeAsync(entity.Id);

        var nominationChanged = await DiffNominationsAsync(entity, input.NomineeEmployeeIds);
        if (nominationChanged)
            await rankBreakdownManager.RefreshForNomineeChangeAsync(entity.Id);

        var dto = toDtoMapper.Map(entity);
        await EnrichSingleDtoAsync(dto, entity);
        return dto;
    }

    /// <summary>
    /// Diffs the requested nominee list against existing nominations for this plan item.
    /// Preserves untouched rows (keeping their IDs, IsReturned flag, and any post-course
    /// result data), deletes nominees no longer in the list (unless a result has already
    /// been entered), and creates new nominations for additions.
    /// Returns true when any row changed.
    /// The whole update runs inside the ABP UoW, so any throw here rolls back the item
    /// field updates too.
    /// </summary>
    private async Task<bool> DiffNominationsAsync(
        TrainingPlanItem entity, List<Guid> newNomineeEmployeeIds)
    {
        var requested = newNomineeEmployeeIds.Distinct().ToHashSet();

        var nomQ = await nominationRepository.GetQueryableAsync();
        var existing = await AsyncExecuter.ToListAsync(
            nomQ.Where(x => x.PlanItemId == entity.Id));
        var existingByEmployee = existing.ToDictionary(x => x.EmployeeId);

        var toRemove = existing.Where(n => !requested.Contains(n.EmployeeId)).ToList();
        var toAdd = requested.Where(id => !existingByEmployee.ContainsKey(id)).ToList();

        if (!toRemove.Any() && !toAdd.Any()) return false;

        var finalCount = existing.Count - toRemove.Count + toAdd.Count;
        if (finalCount == 0)
            throw new Volo.Abp.BusinessException(
                "Training:TrainingPlanItem:AtLeastOneNomineeRequired");

        foreach (var nom in toRemove)
        {
            if (nom.ResultType.HasValue || nom.AttendanceStatus.HasValue)
                throw new Volo.Abp.BusinessException("Training:Nomination:CannotRemoveWithResult")
                    .WithData("EmployeeId", nom.EmployeeId);
            await nominationRepository.DeleteAsync(nom);
        }

        foreach (var employeeId in toAdd)
        {
            await nominationRepository.InsertAsync(
                new Nomination(GuidGenerator.Create(), entity.Id, employeeId, CurrentUser.Id!.Value),
                autoSave: true);
        }

        return true;
    }

    [Authorize(TrainingPermissions.TrainingPlanItem.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessAsync(entity);
        var plan = await planRepository.GetAsync(entity.PlanId);
        if (plan.Status != PlanStatus.Open && plan.Status != PlanStatus.Draft)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:NotInDraftStatus");

        await repository.DeleteAsync(id);
    }

    [Authorize(TrainingPermissions.TrainingPlanItem.Return)]
    public async Task ReturnAsync(Guid id, ReturnReasonDto input)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessAsync(entity);
        var plan = await planRepository.GetAsync(entity.PlanId);
        if (plan.Status != PlanStatus.UnderReview && plan.Status != PlanStatus.TDApproved)
            throw new Volo.Abp.BusinessException("Training:TrainingPlanItem:CannotReturnInThisStatus");

        var noteDto = await planNoteAppService.CreateAsync(new CreatePlanNoteDto
        {
            EntityType = PlanNoteEntityType.PlanItem,
            EntityId = id,
            Note = input.Reason,
            IsReturnReason = true
        });

        entity.IsReturned = true;
        entity.LastReturnNoteId = noteDto.Id;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    private async Task EnrichSingleDtoAsync(TrainingPlanItemDto dto, TrainingPlanItem entity)
    {
        var cn = await courseNameResolver.ResolveAsync(entity.TenantCourseId);
        if (cn != null)
        {
            dto.TenantCourseNameAr = cn.NameAr;
            dto.TenantCourseNameEn = cn.NameEn;
        }

        var emp = await employeeResolver.GetByUserIdAsync(entity.SubmittedById);
        if (emp != null)
        {
            dto.SubmittedByName = emp.FullNameAr;
            dto.SubmittedByRank = emp.Rank?.NameAr;
        }

        if (entity.UnitId.HasValue)
        {
            var ou = await orgUnitRepository.FindAsync(entity.UnitId.Value);
            if (ou != null) dto.UnitName = ou.DisplayName;
        }

        dto.EstimatedCost = await costCalculator.GetEstimatedCostAsync(entity.Id);

        // Nominees count
        var nomQ = await nominationRepository.GetQueryableAsync();
        dto.NomineesCount = await AsyncExecuter.CountAsync(
            nomQ.Where(x => x.PlanItemId == entity.Id && !x.IsReturned));
    }
}
