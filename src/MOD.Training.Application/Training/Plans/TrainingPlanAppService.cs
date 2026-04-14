using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
 

namespace MOD.Training.Training.Plans;

[Authorize(TrainingPermissions.TrainingPlan.Default)]
public class TrainingPlanAppService(
    IRepository<TrainingPlan, Guid> repository,
    IRepository<TrainingPlanItem, Guid> planItemRepository,
    PlanItemCostCalculator costCalculator,
    CourseNameResolver courseNameResolver,
    TrainingPlanToDtoMapper toDtoMapper)
    : ApplicationService, ITrainingPlanAppService
{
    public async Task<TrainingPlanDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        var dto = toDtoMapper.Map(entity);
        await EnrichPlanDtoAsync(dto, id);
        return dto;
    }

    public async Task<PagedResultDto<TrainingPlanDto>> GetListAsync(TrainingPlanGetListInput input)
    {
        var queryable = await repository.GetQueryableAsync();

        if (input.Year.HasValue)
            queryable = queryable.Where(x => x.Year == input.Year.Value);

        var totalCount = await AsyncExecuter.CountAsync(queryable);
        queryable = queryable.OrderByDescending(x => x.Year);
        queryable = queryable.PageBy(input);

        var entities = await AsyncExecuter.ToListAsync(queryable);

        // Batch load item counts
        var planIds = entities.Select(x => x.Id).ToList();
        var itemQueryable = await planItemRepository.GetQueryableAsync();
        var itemCounts = await AsyncExecuter.ToListAsync(
            itemQueryable.Where(x => planIds.Contains(x.PlanId))
                .GroupBy(x => x.PlanId)
                .Select(g => new { PlanId = g.Key, Count = g.Count() }));
        var countMap = itemCounts.ToDictionary(x => x.PlanId, x => x.Count);

        // Batch load total costs per plan
        var allPlanItemIds = await AsyncExecuter.ToListAsync(
            itemQueryable.Where(x => planIds.Contains(x.PlanId))
                .Select(x => new { x.Id, x.PlanId }));

        var allItemIds = allPlanItemIds.Select(x => x.Id).ToList();
        var allCosts = await costCalculator.BatchGetEstimatedCostsAsync(allItemIds);

        var planCostMap = allPlanItemIds
            .GroupBy(x => x.PlanId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(pi => allCosts.GetValueOrDefault(pi.Id, 0)));

        var dtos = entities.Select(e =>
        {
            var dto = toDtoMapper.Map(e);
            dto.ItemCount = countMap.GetValueOrDefault(e.Id, 0);
            dto.TotalEstimatedCost = planCostMap.GetValueOrDefault(e.Id, 0);
            return dto;
        }).ToList();

        return new PagedResultDto<TrainingPlanDto>(totalCount, dtos);
    }

    [Authorize(TrainingPermissions.TrainingPlan.Create)]
    public async Task<TrainingPlanDto> CreateAsync(CreateUpdateTrainingPlanDto input)
    {
        var exists = await AsyncExecuter.AnyAsync(
            (await repository.GetQueryableAsync()).Where(x => x.Year == input.Year));
        if (exists)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:YearAlreadyExists")
                .WithData("Year", input.Year);

        var entity = new TrainingPlan(GuidGenerator.Create(), input.Year);
        entity.OpenDate = input.OpenDate;
        entity.CloseDate = input.CloseDate;

        await repository.InsertAsync(entity, autoSave: true);
        return toDtoMapper.Map(entity);
    }

    [Authorize(TrainingPermissions.TrainingPlan.Update)]
    public async Task<TrainingPlanDto> UpdateAsync(Guid id, CreateUpdateTrainingPlanDto input)
    {
        var entity = await repository.GetAsync(id);
        if (entity.Status != PlanStatus.Draft)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:NotInDraftStatus");

        entity.OpenDate = input.OpenDate;
        entity.CloseDate = input.CloseDate;
        await repository.UpdateAsync(entity, autoSave: true);
        return toDtoMapper.Map(entity);
    }

    [Authorize(TrainingPermissions.TrainingPlan.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        if (entity.Status != PlanStatus.Draft)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:NotInDraftStatus");
        await repository.DeleteAsync(id);
    }

    [Authorize(TrainingPermissions.TrainingPlan.Create)]
    public async Task OpenSubmissionWindowAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        entity.Status = PlanStatus.Open;
        entity.OpenDate ??= DateTime.Now;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    [Authorize(TrainingPermissions.TrainingPlan.Review)]
    public async Task CloseSubmissionWindowAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        entity.Status = PlanStatus.Submitted;
        entity.CloseDate ??= DateTime.Now;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    [Authorize(TrainingPermissions.TrainingPlan.Review)]
    public async Task SubmitForReviewAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        entity.Status = PlanStatus.UnderReview;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    [Authorize(TrainingPermissions.TrainingPlan.Approve)]
    public async Task ApproveAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        await ValidateCostGateAsync(id);
        entity.Status = PlanStatus.TDApproved;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    [Authorize(TrainingPermissions.TrainingPlan.FinalApprove)]
    public async Task FinalApproveAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        await ValidateCostGateAsync(id);
        entity.Status = PlanStatus.THApproved;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    [Authorize(TrainingPermissions.TrainingPlan.Approve)]
    public async Task RejectAsync(Guid id, string? reason)
    {
        var entity = await repository.GetAsync(id);
        entity.Status = PlanStatus.Draft;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    [Authorize(TrainingPermissions.TrainingPlan.Approve)]
    public async Task ReturnToStaffAsync(Guid id, string? reason)
    {
        var entity = await repository.GetAsync(id);
        entity.Status = PlanStatus.Submitted;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    /// <summary>
    /// MOD-13: Cost gate — all external items must have financial items with amounts > 0.
    /// Uses PlanItemCostCalculator instead of stored EstimatedCost field.
    /// </summary>
    private async Task ValidateCostGateAsync(Guid planId)
    {
        var itemQueryable = await planItemRepository.GetQueryableAsync();
        var externalItems = await AsyncExecuter.ToListAsync(
            itemQueryable.Where(x =>
                x.PlanId == planId &&
                x.CourseType != CourseType.Internal));

        var externalIds = externalItems.Select(x => x.Id).ToList();
        var missingCost = await costCalculator.GetItemsMissingCostAsync(externalIds);

        if (missingCost.Any())
        {
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:CostNotEntered")
                .WithData("Count", missingCost.Count);
        }
    }

    private async Task EnrichPlanDtoAsync(TrainingPlanDto dto, Guid planId)
    {
        var itemQueryable = await planItemRepository.GetQueryableAsync();
        dto.ItemCount = await AsyncExecuter.CountAsync(
            itemQueryable.Where(x => x.PlanId == planId));

        var itemIds = await AsyncExecuter.ToListAsync(
            itemQueryable.Where(x => x.PlanId == planId).Select(x => x.Id));
        var costs = await costCalculator.BatchGetEstimatedCostsAsync(itemIds);
        dto.TotalEstimatedCost = costs.Values.Sum();
    }
}
