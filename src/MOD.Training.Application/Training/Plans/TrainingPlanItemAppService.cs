using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Managers;
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
    IRepository<TenantCourseCondition, Guid> tenantConditionRepository,
    IRepository<PlanItemCondition, Guid> planItemConditionRepository,
    IOrganizationUnitRepository orgUnitRepository,
    CourseNameResolver courseNameResolver,
    EmployeeResolver employeeResolver,
    PlanItemCostCalculator costCalculator,
    TrainingPlanItemToDtoMapper toDtoMapper)
    : ApplicationService, ITrainingPlanItemAppService
{
    public async Task<TrainingPlanItemDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        var dto = toDtoMapper.Map(entity);
        await EnrichSingleDtoAsync(dto, entity);
        return dto;
    }

    public async Task<PagedResultDto<TrainingPlanItemDto>> GetListAsync(TrainingPlanItemGetListInput input)
    {
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(x => x.PlanId == input.PlanId);

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

        // Batch resolve all lookups in parallel
        var tcIds = entities.Select(x => x.TenantCourseId).Distinct().ToList();
        var submitterUserIds = entities.Select(x => x.SubmittedById).Distinct().ToList();
        var unitIds = entities.Where(x => x.UnitId.HasValue).Select(x => x.UnitId!.Value).Distinct().ToList();
        var planItemIds = entities.Select(x => x.Id).ToList();

        var courseNames = await courseNameResolver.BatchResolveAsync(tcIds);
        var employees = await employeeResolver.BatchResolveByUserIdsAsync(submitterUserIds);
        var costs = await costCalculator.BatchGetEstimatedCostsAsync(planItemIds);

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

            // Course name
            if (courseNames.TryGetValue(e.TenantCourseId, out var cn))
            {
                dto.TenantCourseNameAr = cn.NameAr;
                dto.TenantCourseNameEn = cn.NameEn;
            }

            // Submitter name + rank
            if (employees.TryGetValue(e.SubmittedById, out var emp))
            {
                dto.SubmittedByName = emp.FullNameAr;
                dto.SubmittedByRank = emp.Rank?.NameAr;
            }

            // Unit name
            if (e.UnitId.HasValue && unitLookup.TryGetValue(e.UnitId.Value, out var unitName))
                dto.UnitName = unitName;

            // Estimated cost (auto-calculated)
            dto.EstimatedCost = costs.GetValueOrDefault(e.Id, 0);

            return dto;
        }).ToList();

        return new PagedResultDto<TrainingPlanItemDto>(totalCount, dtos);
    }

    [Authorize(TrainingPermissions.TrainingPlanItem.Create)]
    public async Task<TrainingPlanItemDto> CreateAsync(CreateUpdateTrainingPlanItemDto input)
    {
        var plan = await planRepository.GetAsync(input.PlanId);
        if (plan.Status != PlanStatus.Open)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:WindowNotOpen");

        // Auto-detect unit from current user's Employee.MainUnitId
        var currentUnitId = await employeeResolver.GetCurrentUserUnitIdAsync();

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
            UnitId = currentUnitId,
            DescriptionAr = input.DescriptionAr,
            DescriptionEn = input.DescriptionEn,
            ObjectivesAr = input.ObjectivesAr,
            ObjectivesEn = input.ObjectivesEn,
            DurationYears = input.DurationYears ,
            DurationMonths = input.DurationMonths ,
            DurationDays = input.DurationDays  ,
            EstimatedDateFrom = input.EstimatedDateFrom,
            EstimatedDateTo = input.EstimatedDateTo,
            FundingSource = input.FundingSource,
        };

        await repository.InsertAsync(entity, autoSave: true);

        // Auto-copy conditions from TenantCourseConditions
        await CopyConditionsAsync(entity.Id, input.TenantCourseId);

        var dto = toDtoMapper.Map(entity);
        await EnrichSingleDtoAsync(dto, entity);
        return dto;
    }

    [Authorize(TrainingPermissions.TrainingPlanItem.Update)]
    public async Task<TrainingPlanItemDto> UpdateAsync(Guid id, CreateUpdateTrainingPlanItemDto input)
    {
        var entity = await repository.GetAsync(id);
        var plan = await planRepository.GetAsync(entity.PlanId);
        if (plan.Status != PlanStatus.Open && plan.Status != PlanStatus.Draft)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:NotInDraftStatus");

        entity.TenantCourseId = input.TenantCourseId;
        entity.CourseType = input.CourseType;
        entity.PreferredQuarter = input.PreferredQuarter;
        entity.Priority = input.Priority;
        entity.Justification = input.Justification;
        entity.DescriptionAr = input.DescriptionAr;
        entity.DescriptionEn = input.DescriptionEn;
        entity.ObjectivesAr = input.ObjectivesAr;
        entity.ObjectivesEn = input.ObjectivesEn;
        entity.DurationYears = input.DurationYears ;
        entity.DurationMonths = input.DurationMonths ;
        entity.DurationDays = input.DurationDays  ;
        entity.EstimatedDateFrom = input.EstimatedDateFrom;
        entity.EstimatedDateTo = input.EstimatedDateTo;
        entity.FundingSource = input.FundingSource;

        await repository.UpdateAsync(entity, autoSave: true);

        var dto = toDtoMapper.Map(entity);
        await EnrichSingleDtoAsync(dto, entity);
        return dto;
    }

    [Authorize(TrainingPermissions.TrainingPlanItem.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        var plan = await planRepository.GetAsync(entity.PlanId);
        if (plan.Status != PlanStatus.Open && plan.Status != PlanStatus.Draft)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:NotInDraftStatus");

        await repository.DeleteAsync(id);
    }

    public async Task<List<PlanItemConditionDto>> GetConditionsAsync(Guid planItemId)
    {
        var queryable = await planItemConditionRepository.GetQueryableAsync();
        var conditions = await AsyncExecuter.ToListAsync(
            queryable.Where(x => x.PlanItemId == planItemId));

        return conditions.Select(c => new PlanItemConditionDto
        {
            Id = c.Id,
            PlanItemId = c.PlanItemId,
            ConditionType = c.ConditionType,
            ConditionValue = c.ConditionValue,
        }).ToList();
    }

    private async Task CopyConditionsAsync(Guid planItemId, Guid tenantCourseId)
    {
        var condQueryable = await tenantConditionRepository.GetQueryableAsync();
        var conditions = await AsyncExecuter.ToListAsync(
            condQueryable.Where(x => x.TenantCourseId == tenantCourseId));

        foreach (var cond in conditions)
        {
            await planItemConditionRepository.InsertAsync(
                new PlanItemCondition(GuidGenerator.Create(), planItemId, cond.ConditionType, cond.ConditionValue),
                autoSave: true);
        }
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
    }

    
}
