using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Plans;

[Authorize(TrainingPermissions.TrainingPlanItem.AssignFinancials)]
public class PlanItemFinancialItemAppService(
    IRepository<PlanItemFinancialItem, Guid> repository,
    IRepository<TrainingPlanItem, Guid> planItemRepository,
    IRepository<FinancialItem, Guid> financialItemRepository,
    PlanItemRankBreakdownManager rankBreakdownManager,
    FinancialItemDefaultResolver defaultResolver,
    PlanItemUnitScope unitScope,
    PlanItemFinancialItemToDtoMapper toDtoMapper)
    : ApplicationService, IPlanItemFinancialItemAppService
{
    public async Task<List<PlanItemFinancialItemDto>> GetListByPlanItemAsync(Guid planItemId)
    {
        await unitScope.EnsureCanAccessPlanItemAsync(planItemId);

        var queryable = await repository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(
            queryable.Where(x => x.PlanItemId == planItemId));

        // Batch load financial item names
        var fiIds = items.Select(x => x.FinancialItemId).Distinct().ToList();
        var fiQueryable = await financialItemRepository.GetQueryableAsync();
        var financialItems = await AsyncExecuter.ToListAsync(
            fiQueryable.Where(x => fiIds.Contains(x.Id)));
        var fiLookup = financialItems.ToDictionary(x => x.Id);

        return items.Select(e =>
        {
            var dto = toDtoMapper.Map(e);
            if (fiLookup.TryGetValue(e.FinancialItemId, out var fi))
                dto.FinancialItemName = fi.NameAr; // Or NameEn based on culture
            return dto;
        }).ToList();
    }

    public async Task<PlanItemFinancialItemDto> CreateAsync(CreateUpdatePlanItemFinancialItemDto input)
    {
        await unitScope.EnsureCanAccessPlanItemAsync(input.PlanItemId);

        var entity = new PlanItemFinancialItem(
            GuidGenerator.Create(),
            input.PlanItemId,
            input.FinancialItemId,
            input.EstimatedAmountOMR);
        entity.EstimatedAmountUSD = input.EstimatedAmountUSD;
        entity.Notes = input.Notes;

        await repository.InsertAsync(entity, autoSave: true);
        return toDtoMapper.Map(entity);
    }

    public async Task<PlanItemFinancialItemDto> UpdateAsync(Guid id, CreateUpdatePlanItemFinancialItemDto input)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessPlanItemAsync(entity.PlanItemId);

        entity.FinancialItemId = input.FinancialItemId;
        entity.EstimatedAmountOMR = input.EstimatedAmountOMR;
        entity.EstimatedAmountUSD = input.EstimatedAmountUSD;
        entity.Notes = input.Notes;

        await repository.UpdateAsync(entity, autoSave: true);
        return toDtoMapper.Map(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessPlanItemAsync(entity.PlanItemId);
        await repository.DeleteAsync(id);
    }

    /// <summary>
    /// Updates the estimated OMR amount for a single financial item.
    /// Called on input blur from the inline editable table.
    /// </summary>
    [Authorize(TrainingPermissions.TrainingPlanItem.Update)]
    public async Task<PlanItemFinancialItemDto> UpdateAmountAsync(Guid id, UpdateAmountDto input)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessPlanItemAsync(entity.PlanItemId);

        entity.EstimatedAmountOMR = input.EstimatedAmountOMR;

        await repository.UpdateAsync(entity, autoSave: true);
        return toDtoMapper.Map(entity);
    }

    /// <summary>
    /// Updates only the notes field for a single financial item.
    /// Called on input blur from the inline editable table.
    /// </summary>
    [Authorize(TrainingPermissions.TrainingPlanItem.Update)]
    public async Task UpdateNotesAsync(Guid id, UpdateNotesDto input)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessPlanItemAsync(entity.PlanItemId);

        entity.Notes = input.Notes;
        await repository.UpdateAsync(entity, autoSave: true);
    }

}
