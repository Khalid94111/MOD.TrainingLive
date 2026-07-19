using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Centers.Dtos;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Nominations;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans;
using MOD.Training.Training.TenantCourses;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Saas.Tenants;

namespace MOD.Training.Training.Centers;

[Authorize(TrainingPermissions.CenterPlanItems.Default)]
public class CenterPlanItemAppService(
    IRepository<TrainingCenterPlanItem, Guid> repository,
    IRepository<TrainingCenterPlanItemUnit, Guid> unitRepository,
    IRepository<TenantCourse, Guid> tenantCourseRepository,
    IRepository<TrainingCenterPlan, Guid> planRepository,
    IRepository<TrainingCenter, Guid> centerRepository,
    IRepository<TrainingPlan, Guid> annualPlanRepository,
    IRepository<TrainingPlanItem, Guid> planItemRepository,
    IRepository<Nomination, Guid> nominationRepository,
    EmployeeResolver employeeResolver,
    IOrganizationUnitRepository orgUnitRepository,
    IRepository<Volo.Saas.Tenants.Tenant, Guid> tenantRepository,
    IDataFilter dataFilter)
    : CrudAppService<
        TrainingCenterPlanItem,
        TrainingCenterPlanItemDto,
        Guid,
        CenterPlanItemGetListInput,
        CreateUpdateCenterPlanItemDto>(repository)
{
    private readonly TrainingCenterPlanItemToDtoMapper _toDtoMapper = new();
    private readonly CreateUpdateCenterPlanItemToEntityMapper _toEntityMapper = new();

    protected override async Task<IQueryable<TrainingCenterPlanItem>> CreateFilteredQueryAsync(
      CenterPlanItemGetListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        query = query.Where(x => x.PlanId == input.PlanId);
        return query;
    }

    public override async Task<PagedResultDto<TrainingCenterPlanItemDto>> GetListAsync(
        CenterPlanItemGetListInput input)
    {
        var query = await CreateFilteredQueryAsync(input);
        var totalCount = await AsyncExecuter.CountAsync(query);

        query = ApplySorting(query, input);
        query = ApplyPaging(query, input);

        var entities = await AsyncExecuter.ToListAsync(query);

        // Batch load tenant course names
        var tenantCourseIds = entities.Select(e => e.TenantCourseId).Distinct().ToList();
        var tenantCourses = (await tenantCourseRepository.WithDetailsAsync(c=>c.CatalogCourse)).Where(
            x => tenantCourseIds.Contains(x.Id));
        var courseMap = tenantCourses.ToDictionary(c => c.Id);

        // Batch load units
        var itemIds = entities.Select(e => e.Id).ToList();
        var allUnits = await unitRepository.GetListAsync(x => itemIds.Contains(x.PlanItemId));
        var unitMap = allUnits.GroupBy(u => u.PlanItemId)
            .ToDictionary(g => g.Key, g => g.Select(u => u.UnitId).ToList());

        var dtos = entities.Select(entity =>
        {
            var dto = _toDtoMapper.Map(entity);
            if (courseMap.TryGetValue(entity.TenantCourseId, out var course))
            {
                dto.TenantCourseName =  course.CatalogCourse.CourseNameAr;
            }
            dto.UnitIds = unitMap.GetValueOrDefault(entity.Id, []);
            return dto;
        }).ToList();

        return new PagedResultDto<TrainingCenterPlanItemDto>(totalCount, dtos);
    }

    [Authorize(TrainingPermissions.CenterPlanItems.Create)]
    public override async Task<TrainingCenterPlanItemDto> CreateAsync(
        CreateUpdateCenterPlanItemDto input)
    {
        var plan = await planRepository.GetAsync(input.PlanId);
        if (plan.Status != CenterPlanStatus.Draft)
        {
            throw new BusinessException("Training:CenterPlanItem:PlanNotDraft");
        }

        var entity = _toEntityMapper.Map(input);

        var existingItems = await Repository.GetListAsync(x => x.PlanId == input.PlanId);
        entity.BatchNumber = existingItems
            .Count(i => i.EstimatedStartDate <= entity.EstimatedStartDate) + 1;

        await Repository.InsertAsync(entity, autoSave: true);
        return await BuildItemDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CenterPlanItems.Edit)]
    public override async Task<TrainingCenterPlanItemDto> UpdateAsync(
        Guid id, CreateUpdateCenterPlanItemDto input)
    {
        var entity = await Repository.GetAsync(id);

        var plan = await planRepository.GetAsync(entity.PlanId);
        if (plan.Status != CenterPlanStatus.Draft)
        {
            throw new BusinessException("Training:CenterPlanItem:PlanNotDraft");
        }

        _toEntityMapper.Map(input, entity);
        await Repository.UpdateAsync(entity, autoSave: true);
        return await BuildItemDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CenterPlanItems.Delete)]
    public override async Task DeleteAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        var plan = await planRepository.GetAsync(entity.PlanId);
        if (plan.Status != CenterPlanStatus.Draft)
        {
            throw new BusinessException("Training:CenterPlanItem:PlanNotDraft");
        }

        await unitRepository.DeleteAsync(x => x.PlanItemId == id);
        await base.DeleteAsync(id);
    }

    [Authorize(TrainingPermissions.CenterPlanItems.SetUnits)]
    public async Task<TrainingCenterPlanItemDto> SetUnitsAsync(
        Guid id, SetPlanItemUnitsDto input)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.BeneficiaryType != BeneficiaryType.Internal)
        {
            throw new BusinessException("Training:CenterPlanItem:UnitsOnlyForInternal");
        }

        await unitRepository.DeleteAsync(x => x.PlanItemId == id);

        var units = input.UnitIds.Select(unitId => new TrainingCenterPlanItemUnit
        {
            PlanItemId = id,
            UnitId = unitId
        }).ToList();

        await unitRepository.InsertManyAsync(units, autoSave: true);

        return await BuildItemDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CenterPlanItems.AdjustCapacity)]
    public async Task<TrainingCenterPlanItemDto> AdjustCapacityAsync(
        Guid id, AdjustCenterPlanItemCapacityDto input)
    {
        var entity = await Repository.GetAsync(id);
        var plan = await planRepository.GetAsync(entity.PlanId);

        // Capacity can only be adjusted after the center plan has been approved.
        if (plan.Status != CenterPlanStatus.Approved)
            throw new BusinessException("Training:CenterPlanItem:CapacityAdjustOnlyApproved");

        // Cannot reduce capacity below already-reserved seats.
        if (input.Capacity < entity.ReservedSeats)
            throw new BusinessException("Training:CenterPlanItem:CapacityBelowReservedSeats")
                .WithData("ReservedSeats", entity.ReservedSeats);

        entity.Capacity = input.Capacity;
        await Repository.UpdateAsync(entity, autoSave: true);

        return await BuildItemDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.TrainingPlanItem.Create)]
    public async Task<ListResultDto<AvailableCenterPlanItemDto>> GetAvailableForAnnualPlanAsync(Guid annualPlanId)
    {
        var annualPlan = await annualPlanRepository.GetAsync(annualPlanId);
        if (annualPlan.Status != PlanStatus.Open && annualPlan.Status != PlanStatus.ReturnedToCreator)
        {
            throw new BusinessException("Training:TrainingPlan:WindowNotOpen");
        }

        var currentTenantId = CurrentTenant.Id;
        var result = new List<AvailableCenterPlanItemDto>();

        // Own-tenant approved center plans for the same year.
        var ownPlans = await planRepository.GetListAsync(
            x => x.TenantId == currentTenantId
                 && x.Year == annualPlan.Year
                 && x.Status == CenterPlanStatus.Approved);
        var ownPlanMap = ownPlans.ToDictionary(p => p.Id);
        var ownCenterMap = (await centerRepository.GetListAsync(
            x => ownPlans.Select(p => p.CenterId).Contains(x.Id)))
            .ToDictionary(c => c.Id);
        var ownItems = ownPlans.Any()
            ? await Repository.GetListAsync(x => ownPlanMap.Keys.Contains(x.PlanId))
            : [];
        var ownItemIds = ownItems.Select(i => i.Id).ToList();
        var ownUnits = ownItemIds.Any()
            ? (await unitRepository.GetListAsync(x => ownItemIds.Contains(x.PlanItemId)))
                .GroupBy(u => u.PlanItemId)
                .ToDictionary(g => g.Key, g => g.Select(u => u.UnitId).ToList())
            : [];

        foreach (var item in ownItems)
        {
            ownPlanMap.TryGetValue(item.PlanId, out var plan);
            ownCenterMap.TryGetValue(plan?.CenterId ?? Guid.Empty, out var center);
            result.Add(MapAvailable(item, center, currentTenantId, ownUnits));
        }

        // Shared items from other tenants.
        using (dataFilter.Disable<IMultiTenant>())
        {
            var itemQuery = await Repository.GetQueryableAsync();
            var planQuery = await planRepository.GetQueryableAsync();

            var sharedQuery =
                from item in itemQuery
                join plan in planQuery on item.PlanId equals plan.Id
                where plan.Year == annualPlan.Year
                      && plan.Status == CenterPlanStatus.Approved
                      && plan.TenantId != currentTenantId
                      && item.BeneficiaryType == BeneficiaryType.Shared
                select new { Item = item, Plan = plan };

            var shared = await AsyncExecuter.ToListAsync(sharedQuery);
            if (shared.Any())
            {
                var sharedCenterIds = shared.Select(s => s.Plan.CenterId).Distinct().ToList();
                var sharedCenters = (await centerRepository.GetListAsync(
                    x => sharedCenterIds.Contains(x.Id))).ToDictionary(c => c.Id);

                var sharedTenantIds = shared.Select(s => s.Plan.TenantId).Distinct().ToList();
                var tenantMap = (await tenantRepository.GetListAsync(
                    t => sharedTenantIds.Contains(t.Id))).ToDictionary(t => t.Id);

                var sharedItemIds = shared.Select(s => s.Item.Id).ToList();
                var sharedUnits = (await unitRepository.GetListAsync(
                    x => sharedItemIds.Contains(x.PlanItemId)))
                    .GroupBy(u => u.PlanItemId)
                    .ToDictionary(g => g.Key, g => g.Select(u => u.UnitId).ToList());

                foreach (var s in shared)
                {
                    sharedCenters.TryGetValue(s.Plan.CenterId, out var center);
                    tenantMap.TryGetValue(s.Plan.TenantId ?? Guid.Empty, out var tenant);
                    result.Add(MapAvailable(s.Item, center, s.Plan.TenantId, sharedUnits, tenant?.Name));
                }
            }
        }

        // Resolve course names for all items in one cross-tenant batch.
        var allTenantCourseIds = result.Select(r => r.TenantCourseId).Distinct().ToList();
        List<TenantCourse> allTenantCourses;
        using (dataFilter.Disable<IMultiTenant>())
        {
            var tcQuery = await tenantCourseRepository.WithDetailsAsync(c => c.CatalogCourse);
            allTenantCourses = await AsyncExecuter.ToListAsync(
                tcQuery.Where(c => allTenantCourseIds.Contains(c.Id)));
        }
        var tenantCourseMap = allTenantCourses.ToDictionary(c => c.Id);

        foreach (var dto in result)
        {
            if (tenantCourseMap.TryGetValue(dto.TenantCourseId, out var tc))
            {
                dto.CatalogCourseId = tc.CatalogCourseId;
                dto.CourseName = tc.CatalogCourse?.CourseNameAr ?? string.Empty;
            }
        }

        return new ListResultDto<AvailableCenterPlanItemDto>(result);
    }

    [Authorize(TrainingPermissions.TrainingCenters.ViewNominations)]
    public async Task<PagedResultDto<CenterPlanNominationDto>> GetNominationsAsync(
        CenterPlanNominationGetListInput input)
    {
        var query = await planItemRepository.GetQueryableAsync();
        query = query.Where(x => x.TrainingCenterPlanItemId != null && x.TenantId == CurrentTenant.Id);

        // Optional filter by center plan.
        if (input.CenterPlanId.HasValue)
        {
            var planItemIdsInPlan = (await Repository.GetListAsync(x => x.PlanId == input.CenterPlanId.Value))
                .Select(x => x.Id)
                .ToList();
            query = query.Where(x => x.TrainingCenterPlanItemId.HasValue
                                      && planItemIdsInPlan.Contains(x.TrainingCenterPlanItemId.Value));
        }

        // Optional filter by center: find all plans for that center and their item ids.
        if (input.CenterId.HasValue)
        {
            var planIds = (await planRepository.GetListAsync(x => x.CenterId == input.CenterId.Value))
                .Select(x => x.Id)
                .ToList();
            var itemIdsInCenter = (await Repository.GetListAsync(x => planIds.Contains(x.PlanId)))
                .Select(x => x.Id)
                .ToList();
            query = query.Where(x => x.TrainingCenterPlanItemId.HasValue
                                      && itemIdsInCenter.Contains(x.TrainingCenterPlanItemId.Value));
        }

        var totalCount = await AsyncExecuter.CountAsync(query);
        query = query.OrderByDescending(x => x.CreationTime).PageBy(input);
        var planItems = await AsyncExecuter.ToListAsync(query);

        var centerPlanItemIds = planItems
            .Select(x => x.TrainingCenterPlanItemId!.Value)
            .Distinct()
            .ToList();
        var centerPlanItems = (await Repository.GetListAsync(x => centerPlanItemIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);

        var centerPlanIds = centerPlanItems.Values.Select(x => x.PlanId).Distinct().ToList();
        var centerPlans = (await planRepository.GetListAsync(x => centerPlanIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);

        var centerIds = centerPlans.Values.Select(x => x.CenterId).Distinct().ToList();
        var centers = (await centerRepository.GetListAsync(x => centerIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);

        var tenantCourseIds = centerPlanItems.Values.Select(x => x.TenantCourseId).Distinct().ToList();
        List<TenantCourse> tenantCourses;
        using (dataFilter.Disable<IMultiTenant>())
        {
            var tcQuery = await tenantCourseRepository.WithDetailsAsync(c => c.CatalogCourse);
            tenantCourses = await AsyncExecuter.ToListAsync(
                tcQuery.Where(c => tenantCourseIds.Contains(c.Id)));
        }
        var tenantCourseMap = tenantCourses.ToDictionary(c => c.Id);

        var planItemIds = planItems.Select(x => x.Id).ToList();
        var nominations = await nominationRepository.GetListAsync(
            x => planItemIds.Contains(x.PlanItemId)
                 && !x.IsReturned
                 && x.Status != NominationStatus.Rejected);

        var nominationsByPlanItem = nominations
            .GroupBy(x => x.PlanItemId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var employeeIds = nominations.Select(x => x.EmployeeId).Distinct().ToList();
        var employees = await employeeResolver.BatchResolveByIdsAsync(employeeIds);

        var unitIds = planItems.Where(x => x.UnitId.HasValue).Select(x => x.UnitId!.Value).Distinct().ToList();
        var unitMap = new Dictionary<Guid, string>();
        foreach (var uid in unitIds)
        {
            var ou = await orgUnitRepository.FindAsync(uid);
            if (ou != null) unitMap[uid] = ou.DisplayName;
        }

        var grouped = planItems
            .GroupBy(x => x.TrainingCenterPlanItemId!.Value)
            .Select(g =>
            {
                var centerPlanItem = centerPlanItems[g.Key];
                centerPlans.TryGetValue(centerPlanItem.PlanId, out var centerPlan);
                centers.TryGetValue(centerPlan?.CenterId ?? Guid.Empty, out var center);
                tenantCourseMap.TryGetValue(centerPlanItem.TenantCourseId, out var tenantCourse);

                return new CenterPlanNominationDto
                {
                    Id = g.Key,
                    CenterPlanId = centerPlanItem.PlanId,
                    CenterName = center?.CenterNameAr ?? string.Empty,
                    CourseName = tenantCourse?.CatalogCourse?.CourseNameAr ?? string.Empty,
                    EstimatedStartDate = centerPlanItem.EstimatedStartDate,
                    EstimatedEndDate = centerPlanItem.EstimatedEndDate,
                    Capacity = centerPlanItem.Capacity,
                    ReservedSeats = centerPlanItem.ReservedSeats,
                    RemainingSeats = Math.Max(0, centerPlanItem.Capacity - centerPlanItem.ReservedSeats),
                    Bookings = g.Select(pi =>
                    {
                        var unitName = pi.UnitId.HasValue && unitMap.TryGetValue(pi.UnitId.Value, out var un)
                            ? un
                            : string.Empty;
                        var nominees = nominationsByPlanItem.GetValueOrDefault(pi.Id) ?? [];

                        return new PlanItemBookingDto
                        {
                            TrainingPlanItemId = pi.Id,
                            UnitId = pi.UnitId,
                            UnitName = unitName,
                            NomineeCount = nominees.Count,
                            Nominees = nominees.Select(n =>
                            {
                                employees.TryGetValue(n.EmployeeId, out var emp);
                                return new CenterPlanBookingNomineeDto
                                {
                                    NominationId = n.Id,
                                    EmployeeId = n.EmployeeId,
                                    ServiceNumber = emp?.ServiceNumber ?? string.Empty,
                                    EmployeeName = emp?.FullNameAr ?? string.Empty,
                                    RankName = emp?.Rank?.NameAr,
                                    UnitName = unitName
                                };
                            }).ToList()
                        };
                    }).ToList()
                };
            })
            .ToList();

        return new PagedResultDto<CenterPlanNominationDto>(totalCount, grouped);
    }

    public override async Task<TrainingCenterPlanItemDto> GetAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        return await BuildItemDtoAsync(entity);
    }

    private async Task<TrainingCenterPlanItemDto> BuildItemDtoAsync(
      TrainingCenterPlanItem entity)
    {
        var dto = _toDtoMapper.Map(entity);

        var course = (await tenantCourseRepository.WithDetailsAsync(c=>c.CatalogCourse)).FirstOrDefault(c=>c.Id== entity.TenantCourseId);
        dto.TenantCourseName = course?.CatalogCourse?.CourseNameAr ?? string.Empty;

        var units = await unitRepository.GetListAsync(x => x.PlanItemId == entity.Id);
        dto.UnitIds = units.Select(u => u.UnitId).ToList();

        return dto;
    }

    private static AvailableCenterPlanItemDto MapAvailable(
        TrainingCenterPlanItem item,
        TrainingCenter? center,
        Guid? sourceTenantId,
        Dictionary<Guid, List<Guid>> unitMap,
        string? sourceTenantName = null)
    {
        return new AvailableCenterPlanItemDto
        {
            Id = item.Id,
            CenterPlanId = item.PlanId,
            CenterId = center?.Id ?? Guid.Empty,
            CenterName = center?.CenterNameAr ?? string.Empty,
            SourceTenantId = sourceTenantId,
            SourceTenantName = sourceTenantName,
            TenantCourseId = item.TenantCourseId,
            EstimatedStartDate = item.EstimatedStartDate,
            EstimatedEndDate = item.EstimatedEndDate,
            DurationWeeks = item.DurationWeeks,
            Capacity = item.Capacity,
            ReservedSeats = item.ReservedSeats,
            RemainingSeats = Math.Max(0, item.Capacity - item.ReservedSeats),
            BeneficiaryType = item.BeneficiaryType,
            EligibleUnitIds = unitMap.GetValueOrDefault(item.Id, [])
        };
    }

    protected override TrainingCenterPlanItemDto MapToGetOutputDto(TrainingCenterPlanItem entity)
        => _toDtoMapper.Map(entity);

    protected override TrainingCenterPlanItemDto MapToGetListOutputDto(TrainingCenterPlanItem entity)
        => _toDtoMapper.Map(entity);
}
