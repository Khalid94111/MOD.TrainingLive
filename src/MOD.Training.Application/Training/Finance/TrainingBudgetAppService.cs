using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance.Dtos;
using MOD.Training.Training.Payments;
using MOD.Training.Training.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Finance;

[Authorize(TrainingPermissions.TrainingBudgets.Default)]
public class TrainingBudgetAppService(
    IRepository<TrainingBudget, Guid> budgetRepo,
    IRepository<FinancialItem, Guid> financialItemRepo,
    IRepository<BudgetReallocation, Guid> reallocationRepo,
    TrainingBudgetToDtoMapper toDtoMapper)
    : ApplicationService, ITrainingBudgetAppService
{
    public async Task<TrainingBudgetDto> GetAsync(Guid id)
    {
        var entity = await budgetRepo.GetAsync(id);
        var fi = await financialItemRepo.GetAsync(entity.FinancialItemId);
        return await MapToDtoAsync(entity, new Dictionary<Guid, FinancialItem> { [fi.Id] = fi });
    }

    public async Task<PagedResultDto<TrainingBudgetDto>> GetListAsync(TrainingBudgetGetListInput input)
    {
        if (input.Year.HasValue)
        {
            var anyExist = await budgetRepo.AnyAsync(x => x.Year == input.Year.Value);
            if (!anyExist)
            {
                var activeItems = await financialItemRepo.GetListAsync(
                    x => x.IsActive);

                foreach (var item in activeItems)
                {
                    await budgetRepo.InsertAsync(new TrainingBudget
                    {
                        Year = input.Year.Value,
                        FinancialItemId = item.Id,
                        TotalAmount = 0,
                        SpentAmount = 0,
                        AlertThreshold = 80
                    }, autoSave: true);
                }
            }
        }

        var queryable = await budgetRepo.GetQueryableAsync();

        if (input.Year.HasValue)
        {
            queryable = queryable.Where(x => x.Year == input.Year.Value);
        }

        if (input.FinancialItemId.HasValue)
        {
            queryable = queryable.Where(x => x.FinancialItemId == input.FinancialItemId.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        queryable = queryable.OrderBy(x => x.FinancialItemId);

        if (input.SkipCount > 0)
            queryable = queryable.Skip(input.SkipCount);
        if (input.MaxResultCount > 0)
            queryable = queryable.Take(input.MaxResultCount);

        var entities = await AsyncExecuter.ToListAsync(queryable);

        var fiIds = entities.Select(e => e.FinancialItemId).Distinct().ToList();
        var financialItems = await financialItemRepo.GetListAsync(x => fiIds.Contains(x.Id));
        var fiMap = financialItems.ToDictionary(f => f.Id);

        // Batch-compute recoverable amounts for all items in this year
        var recoverMap = await GetRecoverableAmountsAsync(entities);

        var dtos = new List<TrainingBudgetDto>();
        foreach (var entity in entities)
        {
            var dto = await MapToDtoAsync(entity, fiMap, recoverMap);
            dtos.Add(dto);
        }

        return new PagedResultDto<TrainingBudgetDto>(totalCount, dtos);
    }

    [Authorize(TrainingPermissions.TrainingBudgets.Edit)]
    public async Task<TrainingBudgetDto> UpdateAsync(Guid id, UpdateAlertThresholdDto input)
    {
        var entity = await budgetRepo.GetAsync(id);
        entity.AlertThreshold = input.AlertThreshold;
        await budgetRepo.UpdateAsync(entity, autoSave: true);

        var fi = await financialItemRepo.GetAsync(entity.FinancialItemId);
        return await MapToDtoAsync(entity, new Dictionary<Guid, FinancialItem> { [fi.Id] = fi });
    }

    private async Task<TrainingBudgetDto> MapToDtoAsync(
        TrainingBudget entity,
        IDictionary<Guid, FinancialItem> fiMap,
        IDictionary<(Guid FinancialItemId, int Year), decimal>? recoverMap = null)
    {
        var dto = toDtoMapper.Map(entity);
        if (fiMap.TryGetValue(entity.FinancialItemId, out var fi))
        {
            dto.FinancialItemNameAr = fi.NameAr;
            dto.FinancialItemNameEn = fi.NameEn;
            dto.IsFinancialItemActive = fi.IsActive;
        }

        dto.Remaining = entity.TotalAmount - entity.SpentAmount;
        dto.SpentPercent = entity.TotalAmount > 0
            ? Math.Round(entity.SpentAmount / entity.TotalAmount * 100, 1)
            : 0;
        dto.IsOverThreshold = dto.SpentPercent > entity.AlertThreshold;

        // Recoverable amount
        if (recoverMap != null && recoverMap.TryGetValue((entity.FinancialItemId, entity.Year), out var recoverAmount))
        {
            dto.AmountToRecoverOMR = recoverAmount;
        }
        else
        {
            dto.AmountToRecoverOMR = await GetRecoverableAmountAsync(entity.FinancialItemId, entity.Year);
        }

        return dto;
    }

    private async Task<decimal> GetRecoverableAmountAsync(Guid financialItemId, int year)
    {
        // Sum pending reallocations targeting this financial item
        var queryable = await reallocationRepo.GetQueryableAsync();
        var sum = await AsyncExecuter.SumAsync(
            queryable.Where(x =>
                x.ToFinancialItemId == financialItemId &&
                x.Status == ReallocationStatus.Pending)
            .Select(x => x.AmountOMR));

        return sum;
    }

    private async Task<IDictionary<(Guid FinancialItemId, int Year), decimal>> GetRecoverableAmountsAsync(
        List<TrainingBudget> budgets)
    {
        var fiIds = budgets.Select(b => b.FinancialItemId).Distinct().ToList();
        var years = budgets.Select(b => b.Year).Distinct().ToList();

        var queryable = await reallocationRepo.GetQueryableAsync();
        var reallocations = await AsyncExecuter.ToListAsync(
            queryable.Where(x =>
                fiIds.Contains(x.ToFinancialItemId) &&
                x.Status == ReallocationStatus.Pending));

        // Group by financial item (year not stored on reallocation, use budget year lookup)
        var result = new Dictionary<(Guid, int), decimal>();
        foreach (var budget in budgets)
        {
            var amount = reallocations
                .Where(r => r.ToFinancialItemId == budget.FinancialItemId)
                .Sum(r => r.AmountOMR);
            result[(budget.FinancialItemId, budget.Year)] = amount;
        }

        return result;
    }
}
