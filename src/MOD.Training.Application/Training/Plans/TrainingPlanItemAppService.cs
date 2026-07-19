using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Catalog;
using MOD.Training.Training.Centers;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Nominations;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans.Dtos;
using MOD.Training.Training.TenantCourses;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Saas.Tenants;

namespace MOD.Training.Training.Plans;

[Authorize(TrainingPermissions.TrainingPlanItem.Default)]
public class TrainingPlanItemAppService(
    IRepository<TrainingPlanItem, Guid> repository,
    IRepository<TrainingPlan, Guid> planRepository,
    IRepository<Nomination, Guid> nominationRepository,
    IRepository<TrainingCenterPlanItem, Guid> centerPlanItemRepository,
    IRepository<TrainingCenterPlan, Guid> centerPlanRepository,
    IRepository<TrainingCenterPlanItemUnit, Guid> centerPlanItemUnitRepository,
    IRepository<TenantCourse, Guid> tenantCourseRepository,
    IRepository<CourseCatalog, Guid> catalogRepository,
    IRepository<TrainingCenter, Guid> centerRepository,
    IOrganizationUnitRepository orgUnitRepository,
    CourseNameResolver courseNameResolver,
    EmployeeResolver employeeResolver,
    PlanItemCostCalculator costCalculator,
    PlanItemRankBreakdownManager rankBreakdownManager,
    PlanItemUnitScope unitScope,
    TrainingCenterPlanReservationManager reservationManager,
    IPlanNoteAppService planNoteAppService,
    TrainingPlanItemToDtoMapper toDtoMapper,
    IRepository<Volo.Saas.Tenants.Tenant, Guid> tenantRepository,
    IDataFilter dataFilter,
    ICurrentTenant currentTenant)
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
        var centerPlanItemIds = entities.Where(x => x.TrainingCenterPlanItemId.HasValue)
            .Select(x => x.TrainingCenterPlanItemId!.Value)
            .Distinct()
            .ToList();

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

        // Batch resolve center-plan source info for internal bookings
        var centerSourceMap = await ResolveCenterSourceMapAsync(centerPlanItemIds);

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

            if (e.TrainingCenterPlanItemId.HasValue &&
                centerSourceMap.TryGetValue(e.TrainingCenterPlanItemId.Value, out var source))
            {
                dto.SourceCenterName = source.CenterName;
                dto.SourceTenantName = source.TenantName;
            }

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

        var (tenantCourseId, courseType, trainingCenterPlanItemId, estimatedFrom, estimatedTo, durationDays)
            = await ResolveCourseSourceAsync(input, plan.Year, resolvedUnitId);

        if (!resolvedUnitId.HasValue && trainingCenterPlanItemId.HasValue)
            throw new BusinessException("Training:TrainingPlanItem:UnitRequiredForCenterBooking");

        if (trainingCenterPlanItemId.HasValue)
            await EnsureNoDuplicateBookingAsync(input.PlanId, trainingCenterPlanItemId.Value, resolvedUnitId);

        var entity = new TrainingPlanItem(
            GuidGenerator.Create(),
            input.PlanId,
            tenantCourseId,
            courseType,
            input.PreferredQuarter,
            input.Priority,
            input.Justification,
            CurrentUser.Id!.Value)
        {
            UnitId = resolvedUnitId,
            TrainingCenterPlanItemId = trainingCenterPlanItemId,
            DescriptionAr = input.DescriptionAr,
            DescriptionEn = input.DescriptionEn,
            ObjectivesAr = input.ObjectivesAr,
            ObjectivesEn = input.ObjectivesEn,
            DurationYears = input.DurationYears,
            DurationMonths = input.DurationMonths,
            DurationDays = durationDays ?? input.DurationDays,
            EstimatedDateFrom = estimatedFrom ?? input.EstimatedDateFrom,
            EstimatedDateTo = estimatedTo ?? input.EstimatedDateTo,
            FundingSource = input.FundingSource,
        };

        await repository.InsertAsync(entity, autoSave: true);

        if (input.NomineeEmployeeIds.Count == 0)
            throw new Volo.Abp.BusinessException("Training:TrainingPlanItem:AtLeastOneNomineeRequired");

        var distinctNominees = input.NomineeEmployeeIds.Distinct().ToList();
        foreach (var employeeId in distinctNominees)
        {
            await nominationRepository.InsertAsync(
                new Nomination(GuidGenerator.Create(), entity.Id, employeeId, CurrentUser.Id!.Value),
                autoSave: true);
        }

        if (trainingCenterPlanItemId.HasValue)
            await reservationManager.ReserveSeatsAsync(trainingCenterPlanItemId.Value, distinctNominees.Count);

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

        if (entity.TrainingCenterPlanItemId.HasValue)
        {
            if (input.TrainingCenterPlanItemId != entity.TrainingCenterPlanItemId)
                throw new BusinessException("Training:TrainingPlanItem:CenterPlanItemIdChanged");

            // Source/course/dates remain locked to the approved center plan item.
        }
        else
        {
            entity.TenantCourseId = input.TenantCourseId;
            entity.CourseType = input.CourseType;
        }

        var oldDays = entity.DurationDays;

        entity.PreferredQuarter = input.PreferredQuarter;
        entity.Priority = input.Priority;
        entity.Justification = input.Justification;
        entity.DescriptionAr = input.DescriptionAr;
        entity.DescriptionEn = input.DescriptionEn;
        entity.ObjectivesAr = input.ObjectivesAr;
        entity.ObjectivesEn = input.ObjectivesEn;
        entity.DurationYears = input.DurationYears;
        entity.DurationMonths = input.DurationMonths;
        entity.DurationDays = entity.TrainingCenterPlanItemId.HasValue ? entity.DurationDays : input.DurationDays;
        entity.EstimatedDateFrom = entity.TrainingCenterPlanItemId.HasValue
            ? entity.EstimatedDateFrom
            : input.EstimatedDateFrom;
        entity.EstimatedDateTo = entity.TrainingCenterPlanItemId.HasValue
            ? entity.EstimatedDateTo
            : input.EstimatedDateTo;
        entity.FundingSource = input.FundingSource;
        entity.IsReturned = false; // saving clears the return flag

        await repository.UpdateAsync(entity, autoSave: true);

        // CHG-07 cascade — DurationDays changed
        if (oldDays != entity.DurationDays)
            await rankBreakdownManager.RefreshForDaysChangeAsync(entity.Id);

        var oldActiveCount = entity.TrainingCenterPlanItemId.HasValue
            ? await CountActiveNomineesAsync(entity.Id)
            : 0;

        var requested = input.NomineeEmployeeIds.Distinct().ToList();
        var nominationChanged = await DiffNominationsAsync(entity, requested);
        if (nominationChanged)
            await rankBreakdownManager.RefreshForNomineeChangeAsync(entity.Id);

        if (entity.TrainingCenterPlanItemId.HasValue)
        {
            var newActiveCount = await CountActiveNomineesAsync(entity.Id);
            var delta = newActiveCount - oldActiveCount;
            if (delta > 0)
                await reservationManager.ReserveSeatsAsync(entity.TrainingCenterPlanItemId.Value, delta);
            else if (delta < 0)
                await reservationManager.ReleaseSeatsAsync(entity.TrainingCenterPlanItemId.Value, -delta);
        }

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

        if (entity.TrainingCenterPlanItemId.HasValue)
        {
            var activeCount = await CountActiveNomineesAsync(entity.Id);
            if (activeCount > 0)
                await reservationManager.ReleaseSeatsAsync(entity.TrainingCenterPlanItemId.Value, activeCount);
        }

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

    private async Task<(Guid TenantCourseId, CourseType CourseType, Guid? TrainingCenterPlanItemId,
        DateTime? EstimatedFrom, DateTime? EstimatedTo, int? DurationDays)>
        ResolveCourseSourceAsync(CreateUpdateTrainingPlanItemDto input, int planYear, Guid? unitId)
    {
        if (input.TrainingCenterPlanItemId.HasValue)
        {
            var centerItem = await centerPlanItemRepository.GetAsync(input.TrainingCenterPlanItemId.Value);
            var centerPlan = await centerPlanRepository.GetAsync(centerItem.PlanId);

            if (centerPlan.Status != CenterPlanStatus.Approved)
                throw new BusinessException("Training:CenterPlan:NotApproved");
            if (centerPlan.Year != planYear)
                throw new BusinessException("Training:CenterPlan:YearMismatch");
            if (centerPlan.TenantId != currentTenant.Id && centerItem.BeneficiaryType != BeneficiaryType.Shared)
                throw new BusinessException("Training:CenterPlanItem:NotAvailable");

            var distinctCount = input.NomineeEmployeeIds.Distinct().Count();
            if (distinctCount > centerItem.Capacity - centerItem.ReservedSeats)
                throw new BusinessException("Training:CenterPlanItem:NoCapacity")
                    .WithData("Requested", distinctCount)
                    .WithData("Remaining", centerItem.Capacity - centerItem.ReservedSeats);

            if (centerPlan.TenantId == currentTenant.Id && centerItem.BeneficiaryType == BeneficiaryType.Internal)
            {
                if (!unitId.HasValue || !await centerPlanItemUnitRepository.AnyAsync(
                        u => u.PlanItemId == centerItem.Id && u.UnitId == unitId.Value))
                    throw new BusinessException("Training:CenterPlanItem:UnitNotEligible");
            }

            var tenantCourseId = centerPlan.TenantId == currentTenant.Id
                ? centerItem.TenantCourseId
                : await ResolveOrCreateLocalTenantCourseFromSourceAsync(centerItem.TenantCourseId);

            return (tenantCourseId, CourseType.Internal, centerItem.Id,
                centerItem.EstimatedStartDate, centerItem.EstimatedEndDate, centerItem.DurationWeeks * 7);
        }

        if (input.CatalogCourseId.HasValue)
        {
            if (input.CourseType != CourseType.ExternalLocal && input.CourseType != CourseType.ExternalInternational)
                throw new BusinessException("Training:TrainingPlanItem:ExternalCourseTypeRequired");

            var tenantCourseId = await ResolveOrCreateLocalTenantCourseAsync(input.CatalogCourseId.Value);
            return (tenantCourseId, input.CourseType, null, null, null, null);
        }

        if (input.TenantCourseId != default)
        {
            var tenantCourse = await tenantCourseRepository.GetAsync(input.TenantCourseId);
            if (tenantCourse.TenantId != currentTenant.Id)
                throw new BusinessException("Training:TenantCourse:NotFound");
            return (tenantCourse.Id, input.CourseType, null, null, null, null);
        }

        throw new BusinessException("Training:TrainingPlanItem:InvalidCourseSource");
    }

    private async Task<Guid> ResolveOrCreateLocalTenantCourseAsync(Guid catalogCourseId)
    {
        var local = await tenantCourseRepository.FirstOrDefaultAsync(
            x => x.TenantId == currentTenant.Id && x.CatalogCourseId == catalogCourseId);
        if (local != null) return local.Id;

        var catalog = await catalogRepository.GetAsync(catalogCourseId);
        if (!catalog.IsActive)
            throw new BusinessException("Training:TenantCourse:CatalogCourseInactive");

        var created = new TenantCourse
        {
            CatalogCourseId = catalogCourseId,
            ResultType = catalog.ResultType,
            RequiresEvaluation = catalog.RequiresEvaluation,
            RequiresProviderEvaluation = catalog.RequiresProviderEvaluation,
            HasCertificate = catalog.HasCertificate,
            EvaluationBlocksCertificate = catalog.EvaluationBlocksCertificate,
            IsActive = true,
            AddedById = CurrentUser.Id!.Value,
            AddedAt = Clock.Now
        };
        created = await tenantCourseRepository.InsertAsync(created, autoSave: true);
        return created.Id;
    }

    private async Task<Guid> ResolveOrCreateLocalTenantCourseFromSourceAsync(Guid sourceTenantCourseId)
    {
        TenantCourse sourceTenantCourse;
        using (dataFilter.Disable<IMultiTenant>())
        {
            sourceTenantCourse = await tenantCourseRepository.GetAsync(sourceTenantCourseId);
        }

        return await ResolveOrCreateLocalTenantCourseAsync(sourceTenantCourse.CatalogCourseId);
    }

    private async Task EnsureNoDuplicateBookingAsync(Guid planId, Guid centerPlanItemId, Guid? unitId)
    {
        var existing = await repository.FirstOrDefaultAsync(
            x => x.PlanId == planId
                 && x.TrainingCenterPlanItemId == centerPlanItemId
                 && x.UnitId == unitId
                 && !x.IsDeleted);
        if (existing != null)
            throw new BusinessException("Training:TrainingPlanItem:AlreadyBookedForUnit");
    }

    private async Task<int> CountActiveNomineesAsync(Guid planItemId)
    {
        var nomQ = await nominationRepository.GetQueryableAsync();
        return await AsyncExecuter.CountAsync(
            nomQ.Where(x => x.PlanItemId == planItemId
                            && !x.IsReturned
                            && x.Status != NominationStatus.Rejected));
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

        if (entity.TrainingCenterPlanItemId.HasValue)
        {
            var source = await ResolveCenterSourceAsync(entity.TrainingCenterPlanItemId.Value);
            dto.SourceCenterName = source?.CenterName;
            dto.SourceTenantName = source?.TenantName;
        }
    }

    private async Task<Dictionary<Guid, (string CenterName, string? TenantName)>> ResolveCenterSourceMapAsync(
        List<Guid> centerPlanItemIds)
    {
        var result = new Dictionary<Guid, (string, string?)>();
        if (!centerPlanItemIds.Any()) return result;

        List<TrainingCenterPlanItem> items;
        using (dataFilter.Disable<IMultiTenant>())
        {
            items = await AsyncExecuter.ToListAsync(
                (await centerPlanItemRepository.GetQueryableAsync())
                .Where(x => centerPlanItemIds.Contains(x.Id)));
        }

        var planIds = items.Select(x => x.PlanId).Distinct().ToList();
        List<TrainingCenterPlan> plans;
        using (dataFilter.Disable<IMultiTenant>())
        {
            plans = await AsyncExecuter.ToListAsync(
                (await centerPlanRepository.GetQueryableAsync())
                .Where(x => planIds.Contains(x.Id)));
        }

        var centerIds = plans.Select(x => x.CenterId).Distinct().ToList();
        List<TrainingCenter> centers;
        using (dataFilter.Disable<IMultiTenant>())
        {
            centers = await AsyncExecuter.ToListAsync(
                (await centerRepository.GetQueryableAsync())
                .Where(x => centerIds.Contains(x.Id)));
        }

        var tenantIds = plans.Where(x => x.TenantId.HasValue).Select(x => x.TenantId!.Value).Distinct().ToList();
        var tenants = tenantIds.Any()
            ? (await AsyncExecuter.ToListAsync(
                (await tenantRepository.GetQueryableAsync())
                .Where(t => tenantIds.Contains(t.Id))))
            : [];

        var planMap = plans.ToDictionary(p => p.Id);
        var centerMap = centers.ToDictionary(c => c.Id);
        var tenantMap = tenants.ToDictionary(t => t.Id);

        foreach (var item in items)
        {
            planMap.TryGetValue(item.PlanId, out var plan);
            centerMap.TryGetValue(plan?.CenterId ?? Guid.Empty, out var center);
            tenantMap.TryGetValue(plan?.TenantId ?? Guid.Empty, out var tenant);
            result[item.Id] = (center?.CenterNameAr ?? string.Empty, tenant?.Name);
        }

        return result;
    }

    private async Task<(string CenterName, string? TenantName)?> ResolveCenterSourceAsync(Guid centerPlanItemId)
    {
        var map = await ResolveCenterSourceMapAsync([centerPlanItemId]);
        return map.TryGetValue(centerPlanItemId, out var value) ? value : null;
    }
}
