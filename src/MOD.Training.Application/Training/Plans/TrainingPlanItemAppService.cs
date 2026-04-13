using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans.Dtos;
using MOD.Training.Training.TenantCourses;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
 
namespace MOD.Training.Training.Plans;

[Authorize(TrainingPermissions.TrainingPlanItem.Default)]
public class TrainingPlanItemAppService(
    IRepository<TrainingPlanItem, Guid> repository,
    IRepository<TrainingPlan, Guid> planRepository,
    IRepository<TenantCourse, Guid> tenantCourseRepository,
    IRepository<TenantCourseCondition, Guid> tenantConditionRepository,
    IRepository<PlanItemCondition, Guid> planItemConditionRepository,
    TrainingPlanItemToDtoMapper toDtoMapper,
    CreateUpdateTrainingPlanItemToEntityMapper toEntityMapper)
    : ApplicationService, ITrainingPlanItemAppService
{
    public async Task<TrainingPlanItemDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        var dto = toDtoMapper.Map(entity);
        await EnrichDtoAsync(dto, entity);
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

        if (!string.IsNullOrWhiteSpace(input.Sorting))
            queryable = queryable.OrderBy(x => x.Priority); // Default sort
        queryable = queryable.PageBy(input);

        var entities = await AsyncExecuter.ToListAsync(queryable);

        // Batch load TenantCourse names
        var tcIds = entities.Select(x => x.TenantCourseId).Distinct().ToList();
        var tcQueryable = await tenantCourseRepository.GetQueryableAsync();
        var tenantCourses = await AsyncExecuter.ToListAsync(
            tcQueryable.Where(x => tcIds.Contains(x.Id)));

        var tcLookup = tenantCourses.ToDictionary(x => x.Id);

        var dtos = entities.Select(e =>
        {
            var dto = toDtoMapper.Map(e);
            if (tcLookup.TryGetValue(e.TenantCourseId, out var tc))
            {
                // TODO: resolve catalog name via join — for now use TenantCourseId
                dto.TenantCourseName = tc.Id.ToString(); // Will be resolved with catalog join
            }
            return dto;
        }).ToList();

        return new PagedResultDto<TrainingPlanItemDto>(totalCount, dtos);
    }

    [Authorize(TrainingPermissions.TrainingPlanItem.Create)]
    public async Task<TrainingPlanItemDto> CreateAsync(CreateUpdateTrainingPlanItemDto input)
    {
        // Validate plan is in Open status (submission window open)
        var plan = await planRepository.GetAsync(input.PlanId);
        if (plan.Status != PlanStatus.Open)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:WindowNotOpen");

        var entity = new TrainingPlanItem(
            GuidGenerator.Create(),
            input.PlanId,
            input.TenantCourseId,
            input.CourseType,
            input.PreferredQuarter,
            input.Priority,
            input.OfficersCount,
            input.EnlistedCount,
            input.Justification,
            CurrentUser.Id!.Value);

        // Copy overridable fields from input
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

        // Auto-set UnitId from current user context
        // TODO: resolve from CurrentUser's OrgUnit via HR integration

        await repository.InsertAsync(entity, autoSave: true);

        // Auto-copy conditions from TenantCourseConditions
        await CopyConditionsFromTenantCourseAsync(entity.Id, input.TenantCourseId);

        return toDtoMapper.Map(entity);
    }

    [Authorize(TrainingPermissions.TrainingPlanItem.Update)]
    public async Task<TrainingPlanItemDto> UpdateAsync(Guid id, CreateUpdateTrainingPlanItemDto input)
    {
        var entity = await repository.GetAsync(id);

        // Validate plan is still editable
        var plan = await planRepository.GetAsync(entity.PlanId);
        if (plan.Status != PlanStatus.Open && plan.Status != PlanStatus.Draft)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:NotInDraftStatus");

        toEntityMapper.Map(input, entity);
        entity.Capacity = input.OfficersCount + input.EnlistedCount;

        await repository.UpdateAsync(entity, autoSave: true);
        return toDtoMapper.Map(entity);
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

    // Staff enters estimated cost during review
    [Authorize(TrainingPermissions.TrainingPlanItem.AssignFinancials)]
    public async Task UpdateEstimatedCostAsync(Guid id, UpdateEstimatedCostDto input)
    {
        var entity = await repository.GetAsync(id);
        entity.EstimatedCost = input.EstimatedCost;
        await repository.UpdateAsync(entity, autoSave: true);
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
            ConditionValue = c.ConditionValue
        }).ToList();
    }

    private async Task CopyConditionsFromTenantCourseAsync(Guid planItemId, Guid tenantCourseId)
    {
        var condQueryable = await tenantConditionRepository.GetQueryableAsync();
        var conditions = await AsyncExecuter.ToListAsync(
            condQueryable.Where(x => x.TenantCourseId == tenantCourseId));

        foreach (var cond in conditions)
        {
            await planItemConditionRepository.InsertAsync(
                new PlanItemCondition(
                    GuidGenerator.Create(),
                    planItemId,
                    cond.ConditionType,
                    cond.ConditionValue),
                autoSave: true);
        }
    }

    private async Task EnrichDtoAsync(TrainingPlanItemDto dto, TrainingPlanItem entity)
    {
        // Resolve TenantCourse name
        var tc = await tenantCourseRepository.FindAsync(entity.TenantCourseId);
        if (tc != null)
        {
            dto.TenantCourseName = tc.Id.ToString(); // TODO: resolve catalog name
        }
    }
}
