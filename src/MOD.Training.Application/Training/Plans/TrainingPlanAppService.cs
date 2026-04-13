using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training;
using MOD.Training.Training.Enums;
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
    TrainingPlanToDtoMapper toDtoMapper,
    CreateUpdateTrainingPlanToEntityMapper toEntityMapper)
    : ApplicationService, ITrainingPlanAppService
{
    public async Task<TrainingPlanDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        var dto = toDtoMapper.Map(entity);
        dto.ItemCount = await AsyncExecuter.CountAsync(
            (await planItemRepository.GetQueryableAsync()).Where(x => x.PlanId == id));
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

        var dtos = entities.Select(e =>
        {
            var dto = toDtoMapper.Map(e);
            dto.ItemCount = countMap.GetValueOrDefault(e.Id, 0);
            return dto;
        }).ToList();

        return new PagedResultDto<TrainingPlanDto>(totalCount, dtos);
    }

    [Authorize(TrainingPermissions.TrainingPlan.Create)]
    public async Task<TrainingPlanDto> CreateAsync(CreateUpdateTrainingPlanDto input)
    {
        // Year uniqueness per tenant
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

    // Step 1: TD opens submission window
    [Authorize(TrainingPermissions.TrainingPlan.Create)]
    public async Task OpenSubmissionWindowAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        entity.Status = PlanStatus.Open;
        entity.OpenDate ??= DateTime.Now;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    // Step 4: Staff closes window
    [Authorize(TrainingPermissions.TrainingPlan.Review)]
    public async Task CloseSubmissionWindowAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        entity.Status = PlanStatus.Submitted;
        entity.CloseDate ??= DateTime.Now;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    // Step 4: Staff submits for review after assigning financials
    [Authorize(TrainingPermissions.TrainingPlan.Review)]
    public async Task SubmitForReviewAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        entity.Status = PlanStatus.UnderReview;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    // Step 5: TD approves (cost gate)
    [Authorize(TrainingPermissions.TrainingPlan.Approve)]
    public async Task ApproveAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        await ValidateCostGateAsync(id);
        entity.Status = PlanStatus.TDApproved;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    // Step 6: TH final approval (cost gate)
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

    private async Task ValidateCostGateAsync(Guid planId)
    {
        // MOD-13: all external items must have EstimatedCost > 0
        var itemQueryable = await planItemRepository.GetQueryableAsync();
        var hasItemsWithoutCost = await AsyncExecuter.AnyAsync(
            itemQueryable.Where(x =>
                x.PlanId == planId &&
                x.CourseType != CourseType.Internal &&
                (x.EstimatedCost == null || x.EstimatedCost <= 0)));

        if (hasItemsWithoutCost)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:CostNotEntered");
    }
}
