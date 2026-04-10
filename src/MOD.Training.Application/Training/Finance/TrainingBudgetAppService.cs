using System;
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
    TrainingBudgetToDtoMapper toDtoMapper,
    CreateUpdateTrainingBudgetToEntityMapper toEntityMapper)
    : ApplicationService, ITrainingBudgetAppService
{
    public async Task<TrainingBudgetDto> GetAsync(Guid id)
    {
        var entity = await budgetRepo.GetAsync(id);
        return MapToDto(entity);
    }

    public async Task<PagedResultDto<TrainingBudgetDto>> GetListAsync(TrainingBudgetGetListInput input)
    {
        var queryable = await budgetRepo.GetQueryableAsync();

        if (input.Year.HasValue)
        {
            queryable = queryable.Where(x => x.Year == input.Year.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        queryable = queryable.OrderBy(x => x.BudgetType);

        if (input.SkipCount > 0)
            queryable = queryable.Skip(input.SkipCount);
        if (input.MaxResultCount > 0)
            queryable = queryable.Take(input.MaxResultCount);

        var entities = await AsyncExecuter.ToListAsync(queryable);
        var dtos = entities.Select(MapToDto).ToList();

        return new PagedResultDto<TrainingBudgetDto>(totalCount, dtos);
    }

    [Authorize(TrainingPermissions.TrainingBudgets.Create)]
    public async Task<TrainingBudgetDto> CreateAsync(CreateUpdateTrainingBudgetDto input)
    {
        // Validate: unique (Year + BudgetType) per tenant
        var exists = await budgetRepo.AnyAsync(x =>
            x.Year == input.Year && x.BudgetType == input.BudgetType);

        if (exists)
        {
            throw new BusinessException("Training:Budget:AlreadyExists");
        }

        var entity = toEntityMapper.Map(input);
        await budgetRepo.InsertAsync(entity);
        return MapToDto(entity);
    }

    [Authorize(TrainingPermissions.TrainingBudgets.Edit)]
    public async Task<TrainingBudgetDto> UpdateAsync(Guid id, CreateUpdateTrainingBudgetDto input)
    {
        var entity = await budgetRepo.GetAsync(id);

        // Validate: unique (Year + BudgetType) per tenant (exclude self)
        var exists = await budgetRepo.AnyAsync(x =>
            x.Id != id && x.Year == input.Year && x.BudgetType == input.BudgetType);

        if (exists)
        {
            throw new BusinessException("Training:Budget:AlreadyExists");
        }

        toEntityMapper.Map(input, entity);
        await budgetRepo.UpdateAsync(entity);
        return MapToDto(entity);
    }

    [Authorize(TrainingPermissions.TrainingBudgets.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await budgetRepo.DeleteAsync(id);
    }

    private TrainingBudgetDto MapToDto(TrainingBudget entity)
    {
        var dto = toDtoMapper.Map(entity);
        dto.Remaining = entity.TotalAmount - entity.SpentAmount;
        dto.SpentPercent = entity.TotalAmount > 0
            ? Math.Round(entity.SpentAmount / entity.TotalAmount * 100, 1)
            : 0;
        dto.IsOverThreshold = dto.SpentPercent > entity.AlertThreshold;
        return dto;
    }
}
