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
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Plans;

[Authorize(TrainingPermissions.TrainingPlan.Default)]
public class TrainingPlanAppService(
    IRepository<TrainingPlan, Guid> repository,
    IRepository<TrainingPlanItem, Guid> planItemRepository,
    IRepository<Nomination, Guid> nominationRepository,
    PlanItemCostCalculator costCalculator,
    CourseNameResolver courseNameResolver,
    BudgetRecalculatorManager budgetRecalculator,
    PlanItemUnitScope unitScope,
    IPlanNoteAppService planNoteAppService,
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

        // Batch load item counts — counts and costs are scoped to the current user's
        // unit for UTM/UGM so the figures in the list match what they actually see.
        var planIds = entities.Select(x => x.Id).ToList();
        var itemQueryable = await planItemRepository.GetQueryableAsync();
        itemQueryable = await unitScope.ApplyScopeAsync(itemQueryable);
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
    public async Task ReopenSubmissionWindowAsync(Guid id, ReopenSubmissionWindowDto input)
    {
        var entity = await repository.GetAsync(id);
        if (entity.Status != PlanStatus.Submitted)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:ReopenWindowNotAllowed");

        entity.Status = PlanStatus.Open;
        entity.OpenDate = input.OpenDate;
        entity.CloseDate = input.CloseDate;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    [Authorize(TrainingPermissions.TrainingPlan.Review)]
    public async Task SubmitForReviewAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        if (entity.Status != PlanStatus.Submitted)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:NotInSubmittedStatus");

        entity.Status = PlanStatus.UnderReview;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    [Authorize(TrainingPermissions.TrainingPlan.Approve)]
    public async Task ApproveAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        // Defensive guard: the TD-approval transition must come from UnderReview only.
        // Prevents any caller (including a misrouted frontend button) from flipping a
        // Submitted or ReturnedToCreator plan straight to TDApproved and skipping staff review.
        if (entity.Status != PlanStatus.UnderReview)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:NotInUnderReviewStatus");
        await ValidateCostGateAsync(id);
        await ValidateNoUnresolvedReturnsAsync(id);
        entity.Status = PlanStatus.TDApproved;
        await repository.UpdateAsync(entity, autoSave: true);
    }
    [Authorize(TrainingPermissions.TrainingPlan.FinalApprove)]
    public async Task FinalApproveAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);

        // Cost gate check
        await ValidateCostGateAsync(id);

        // Approve
        entity.Status = PlanStatus.THApproved;
        await repository.UpdateAsync(entity, autoSave: true);

        // Recalculate budgets from approved plan items
        await budgetRecalculator.RecalculateAsync(id, entity.Year);
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

    [Authorize(TrainingPermissions.TrainingPlan.ReturnToCreator)]
    public async Task ReturnToCreatorAsync(Guid id, ReturnReasonDto input)
    {
        var entity = await repository.GetAsync(id);
        if (entity.Status != PlanStatus.UnderReview && entity.Status != PlanStatus.TDApproved)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:CannotReturnInThisStatus");

        await planNoteAppService.CreateAsync(new CreatePlanNoteDto
        {
            EntityType = PlanNoteEntityType.Plan,
            EntityId = id,
            Note = input.Reason,
            IsReturnReason = true
        });

        entity.Status = PlanStatus.ReturnedToCreator;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    [Authorize(TrainingPermissions.TrainingPlan.Resubmit)]
    public async Task ResubmitAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        if (entity.Status != PlanStatus.ReturnedToCreator)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:NotInReturnedStatus");

        await ValidateNoUnresolvedReturnsAsync(id);

        entity.Status = PlanStatus.UnderReview;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    private async Task ValidateNoUnresolvedReturnsAsync(Guid planId)
    {
        var itemQ = await planItemRepository.GetQueryableAsync();
        var hasReturnedItem = await AsyncExecuter.AnyAsync(
            itemQ.Where(x => x.PlanId == planId && x.IsReturned));
        if (hasReturnedItem)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:UnresolvedReturnedItems");

        var nomQ = await nominationRepository.GetQueryableAsync();
        var hasReturnedNomination = await AsyncExecuter.AnyAsync(
            nomQ.Where(x => x.PlanItem!.PlanId == planId && x.IsReturned));
        if (hasReturnedNomination)
            throw new Volo.Abp.BusinessException("Training:TrainingPlan:UnresolvedReturnedNominations");
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
        itemQueryable = await unitScope.ApplyScopeAsync(itemQueryable);

        dto.ItemCount = await AsyncExecuter.CountAsync(
            itemQueryable.Where(x => x.PlanId == planId));

        var itemIds = await AsyncExecuter.ToListAsync(
            itemQueryable.Where(x => x.PlanId == planId).Select(x => x.Id));
        var costs = await costCalculator.BatchGetEstimatedCostsAsync(itemIds);
        dto.TotalEstimatedCost = costs.Values.Sum();
    }
}
