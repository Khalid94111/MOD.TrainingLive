using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training;
using MOD.Training.Training.Finance;
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
    PlanItemFinancialItemToDtoMapper toDtoMapper)
    : ApplicationService, IPlanItemFinancialItemAppService
{
    public async Task<List<PlanItemFinancialItemDto>> GetListByPlanItemAsync(Guid planItemId)
    {
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
        entity.FinancialItemId = input.FinancialItemId;
        entity.EstimatedAmountOMR = input.EstimatedAmountOMR;
        entity.EstimatedAmountUSD = input.EstimatedAmountUSD;
        entity.Notes = input.Notes;

        await repository.UpdateAsync(entity, autoSave: true);
        return toDtoMapper.Map(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        await repository.DeleteAsync(id);
    }

    // MOD-15: Auto-fill financial items from CourseTypeFinancialItemDefaults
    public async Task AutoFillFromDefaultsAsync(Guid planItemId)
    {
        var planItem = await planItemRepository.GetAsync(planItemId);

        // Get defaults for this course type
        var defaultsQueryable = await defaultsRepository.GetQueryableAsync();
        var defaults = await AsyncExecuter.ToListAsync(
            defaultsQueryable.Where(x => x.CourseType == planItem.CourseType));

        // Remove existing auto-filled items for this plan item
        var existingQueryable = await repository.GetQueryableAsync();
        var existing = await AsyncExecuter.ToListAsync(
            existingQueryable.Where(x => x.PlanItemId == planItemId));
        foreach (var item in existing)
            await repository.DeleteAsync(item);

        // Create new items from defaults (amounts blank — Staff enters manually)
        foreach (var def in defaults)
        {
            await repository.InsertAsync(
                new PlanItemFinancialItem(
                    GuidGenerator.Create(),
                    planItemId,
                    def.FinancialItemId,
                    0), // Amount blank — Staff enters
                autoSave: true);
        }
    }
}
