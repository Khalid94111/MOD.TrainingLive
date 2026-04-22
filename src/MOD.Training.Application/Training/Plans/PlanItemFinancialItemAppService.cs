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
    IRepository<CourseTypeFinancialItemDefault, Guid> defaultsRepository,
    IRepository<ExchangeRate, Guid> exchangeRate,
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

    // MOD-15: Auto-fill financial items from CourseTypeFinancialItemDefaults (non-destructive)
    public async Task AutoFillFromDefaultsAsync(Guid planItemId)
    {
        var planItem = await planItemRepository.GetAsync(planItemId);
        await unitScope.EnsureCanAccessAsync(planItem);

        var defaultsQueryable = await defaultsRepository.GetQueryableAsync();
        var defaults = await AsyncExecuter.ToListAsync(
            defaultsQueryable.Where(x => x.CourseType == planItem.CourseType));

        var existingQueryable = await repository.GetQueryableAsync();
        var existingFiIds = (await AsyncExecuter.ToListAsync(
            existingQueryable.Where(x => x.PlanItemId == planItemId)
                .Select(x => x.FinancialItemId))).ToHashSet();

        // Active USD→OMR rate (same lookup as UpdateAmountAsync); fetched once.
        var rateQueryable = await exchangeRate.GetQueryableAsync();
        var activeRate = await AsyncExecuter.FirstOrDefaultAsync(
            rateQueryable
                .Where(r => r.FromCurrency == "USD" && r.ToCurrency == "OMR" && r.IsActive)
                .OrderByDescending(r => r.SetAt));

        foreach (var def in defaults)
        {
            if (existingFiIds.Contains(def.FinancialItemId)) continue; // skip if already added

            var fi = await financialItemRepository.GetAsync(def.FinancialItemId);

            // CHG-07 — per-nominee items get 0 here; the rank breakdown (InitializeAsync)
            // owns the OMR total. Flat and per-day items are computed up-front with the
            // full formula so the grid reflects a real estimate immediately.
            decimal initialOmr = fi.IsPerNominee
                ? 0m
                : defaultResolver.ComputeSubtotal(
                    fi.DefaultAmountOMR,
                    fi.IsPerDay, fi.IsPerNominee,
                    planItem.DurationDays,
                    fi.ExtraDaysBefore, fi.ExtraDaysAfter,
                    nomineeCount: 1);

            var pifi = new PlanItemFinancialItem(
                GuidGenerator.Create(), planItemId, def.FinancialItemId, initialOmr);

            // Mirror UpdateAmountAsync: compute USD only for non-per-nominee items,
            // since per-nominee OMR is not finalized until InitializeAsync runs.
            if (!fi.IsPerNominee && activeRate != null && activeRate.Rate > 0)
            {
                pifi.EstimatedAmountUSD = Math.Round(initialOmr / activeRate.Rate, 2);
            }

            await repository.InsertAsync(pifi, autoSave: true);

            // CHG-03 + CHG-07 — initialize per-rank breakdown if applicable
            if (fi.IsPerNominee)
                await rankBreakdownManager.InitializeAsync(pifi.Id);
        }
    }

    /// <summary>
    /// Updates the estimated OMR amount for a single financial item.
    /// Auto-calculates USD from the active exchange rate.
    /// Called on input blur from the inline editable table.
    /// </summary>
    [Authorize(TrainingPermissions.TrainingPlanItem.Update)]
    public async Task<PlanItemFinancialItemDto> UpdateAmountAsync(Guid id, UpdateAmountDto input)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessPlanItemAsync(entity.PlanItemId);

        entity.EstimatedAmountOMR = input.EstimatedAmountOMR;

        // Auto-calculate USD from OMR using active exchange rate
        var rateQueryable = await exchangeRate.GetQueryableAsync();
        var rate = await AsyncExecuter.FirstOrDefaultAsync(
            rateQueryable
                .Where(r => r.FromCurrency == "USD" && r.ToCurrency == "OMR" && r.IsActive)
                .OrderByDescending(r => r.SetAt));

        if (rate != null && rate.Rate > 0)
        {
            entity.EstimatedAmountUSD = Math.Round(input.EstimatedAmountOMR / rate.Rate, 2);
        }

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
