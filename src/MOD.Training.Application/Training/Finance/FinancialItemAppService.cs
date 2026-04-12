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

[Authorize(TrainingPermissions.FinancialItems.Default)]
public class FinancialItemAppService(
    IRepository<FinancialItem, Guid> financialItemRepo,
    IRepository<TrainingBudget, Guid> budgetRepo,
    FinancialItemToDtoMapper toDtoMapper,
    CreateUpdateFinancialItemToEntityMapper toEntityMapper)
    : ApplicationService, IFinancialItemAppService
{
    public async Task<FinancialItemDto> GetAsync(Guid id)
    {
        var entity = await financialItemRepo.GetAsync(id);
        return toDtoMapper.Map(entity);
    }

    public async Task<PagedResultDto<FinancialItemDto>> GetListAsync(FinancialItemGetListInput input)
    {
        var queryable = await financialItemRepo.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            queryable = queryable.Where(x =>
                x.NameAr.Contains(input.Filter) ||
                x.NameEn.Contains(input.Filter) ||
                x.Code.Contains(input.Filter) ||
                x.VoteCode.Contains(input.Filter));
        }

        if (input.IsActive.HasValue)
        {
            queryable = queryable.Where(x => x.IsActive == input.IsActive.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        queryable = queryable.OrderBy(x => x.ParentId).ThenBy(x => x.NameAr);

        if (input.SkipCount > 0)
            queryable = queryable.Skip(input.SkipCount);
        if (input.MaxResultCount > 0)
            queryable = queryable.Take(input.MaxResultCount);

        var entities = await AsyncExecuter.ToListAsync(queryable);
        var dtos = entities.Select(toDtoMapper.Map).ToList();

        return new PagedResultDto<FinancialItemDto>(totalCount, dtos);
    }

    [Authorize(TrainingPermissions.FinancialItems.Create)]
    public async Task<FinancialItemDto> CreateAsync(CreateUpdateFinancialItemDto input)
    {
        // Validate: max 2 levels (sub-items cannot have children)
        if (input.ParentId.HasValue)
        {
            var parent = await financialItemRepo.GetAsync(input.ParentId.Value);
            if (parent.ParentId.HasValue)
            {
                throw new BusinessException("Training:FinancialItem:MaxTwoLevels");
            }
        }

        var entity = toEntityMapper.Map(input);
        entity.Code = GenerateCode(input.NameEn);
        entity.IsGeneral = !input.ParentId.HasValue; // Parents are general, sub-items are not

        await financialItemRepo.InsertAsync(entity, autoSave: true);

        if (!input.ParentId.HasValue)
        {
            var currentYear = DateTime.Now.Year;
            var budgetExists = await budgetRepo.AnyAsync(
                x => x.Year == currentYear && x.FinancialItemId == entity.Id);

            if (!budgetExists)
            {
                await budgetRepo.InsertAsync(new TrainingBudget
                {
                    Year = currentYear,
                    FinancialItemId = entity.Id,
                    TotalAmount = 0,
                    SpentAmount = 0,
                    AlertThreshold = 80
                }, autoSave: true);
            }
        }

        return toDtoMapper.Map(entity);
    }

    [Authorize(TrainingPermissions.FinancialItems.Edit)]
    public async Task<FinancialItemDto> UpdateAsync(Guid id, CreateUpdateFinancialItemDto input)
    {
        var entity = await financialItemRepo.GetAsync(id);
        toEntityMapper.Map(input, entity);
        entity.Code = GenerateCode(input.NameAr);
        entity.IsGeneral = !input.ParentId.HasValue;

        await financialItemRepo.UpdateAsync(entity);
        return toDtoMapper.Map(entity);
    }

    [Authorize(TrainingPermissions.FinancialItems.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        // Check if parent has children
        var hasChildren = await financialItemRepo.AnyAsync(x => x.ParentId == id);
        if (hasChildren)
        {
            throw new BusinessException("Training:FinancialItem:HasChildren");
        }

        await financialItemRepo.DeleteAsync(id);
    }

    public async Task<List<FinancialItemDto>> GetSubItemsAsync()
    {
        var queryable = await financialItemRepo.GetQueryableAsync();
        var subItems = await AsyncExecuter.ToListAsync(
            queryable.Where(x => x.ParentId != null && x.IsActive)
                     .OrderBy(x => x.NameAr));

        return subItems.Select(toDtoMapper.Map).ToList();
    }

    private static string GenerateCode(string nameAr)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) return string.Empty;

        var words = nameAr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var letters = words
            .Where(w => w.Length > 0)
            .Select(w => w[0].ToString());

        return string.Join(".", letters);
    }
}
