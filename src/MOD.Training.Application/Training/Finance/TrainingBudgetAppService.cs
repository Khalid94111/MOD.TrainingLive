using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Finance.Dtos;
using MOD.Training.Training.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Finance;

[Authorize(TrainingPermissions.TrainingBudgets.Default)]
public class TrainingBudgetAppService(
    IRepository<TrainingBudget, Guid> budgetRepo,
    IRepository<FinancialItem, Guid> financialItemRepo,
    TrainingBudgetToDtoMapper toDtoMapper,
    CreateUpdateTrainingBudgetToEntityMapper toEntityMapper)
    : ApplicationService, ITrainingBudgetAppService
{
    public async Task<TrainingBudgetDto> GetAsync(Guid id)
    {
        var entity = await budgetRepo.GetAsync(id);
        var fi = await financialItemRepo.GetAsync(entity.FinancialItemId);
        return MapToDto(entity, new Dictionary<Guid, FinancialItem> { [fi.Id] = fi });
    }

    public async Task<PagedResultDto<TrainingBudgetDto>> GetListAsync(TrainingBudgetGetListInput input)
    {
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

        var dtos = entities.Select(e => MapToDto(e, fiMap)).ToList();

        return new PagedResultDto<TrainingBudgetDto>(totalCount, dtos);
    }

    [Authorize(TrainingPermissions.TrainingBudgets.Create)]
    public async Task<TrainingBudgetDto> CreateAsync(CreateUpdateTrainingBudgetDto input)
    {
        await ValidateFinancialItemAsync(input.FinancialItemId);

        var exists = await budgetRepo.AnyAsync(x =>
            x.Year == input.Year && x.FinancialItemId == input.FinancialItemId);

        if (exists)
        {
            throw new BusinessException("Training:TrainingBudget:AlreadyExists");
        }

        var entity = toEntityMapper.Map(input);
        await budgetRepo.InsertAsync(entity);

        var fi = await financialItemRepo.GetAsync(entity.FinancialItemId);
        return MapToDto(entity, new Dictionary<Guid, FinancialItem> { [fi.Id] = fi });
    }

    [Authorize(TrainingPermissions.TrainingBudgets.Edit)]
    public async Task<TrainingBudgetDto> UpdateAsync(Guid id, CreateUpdateTrainingBudgetDto input)
    {
        var entity = await budgetRepo.GetAsync(id);

        await ValidateFinancialItemAsync(input.FinancialItemId);

        var exists = await budgetRepo.AnyAsync(x =>
            x.Id != id && x.Year == input.Year && x.FinancialItemId == input.FinancialItemId);

        if (exists)
        {
            throw new BusinessException("Training:TrainingBudget:AlreadyExists");
        }

        toEntityMapper.Map(input, entity);
        await budgetRepo.UpdateAsync(entity);

        var fi = await financialItemRepo.GetAsync(entity.FinancialItemId);
        return MapToDto(entity, new Dictionary<Guid, FinancialItem> { [fi.Id] = fi });
    }

    [Authorize(TrainingPermissions.TrainingBudgets.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await budgetRepo.DeleteAsync(id);
    }

    private async Task ValidateFinancialItemAsync(Guid financialItemId)
    {
        var financialItem = await financialItemRepo.GetAsync(financialItemId);
        if (financialItem.ParentId != null)
        {
            throw new BusinessException("Training:TrainingBudget:MustBeParentItem");
        }
    }

    private TrainingBudgetDto MapToDto(TrainingBudget entity, IDictionary<Guid, FinancialItem> fiMap)
    {
        var dto = toDtoMapper.Map(entity);
        if (fiMap.TryGetValue(entity.FinancialItemId, out var fi))
        {
            dto.FinancialItemNameAr = fi.NameAr;
            dto.FinancialItemNameEn = fi.NameEn;
        }
        dto.Remaining = entity.TotalAmount - entity.SpentAmount;
        dto.SpentPercent = entity.TotalAmount > 0
            ? Math.Round(entity.SpentAmount / entity.TotalAmount * 100, 1)
            : 0;
        dto.IsOverThreshold = dto.SpentPercent > entity.AlertThreshold;
        return dto;
    }
}
