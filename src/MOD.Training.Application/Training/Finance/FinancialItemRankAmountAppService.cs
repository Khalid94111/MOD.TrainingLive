using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Finance.Dtos;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Finance;

[Authorize(TrainingPermissions.FinancialItemRankAmount.Default)]
public class FinancialItemRankAmountAppService(
    IRepository<FinancialItemRankAmount, Guid> repository,
    EmployeeResolver employeeResolver,
    FinancialItemRankAmountToDtoMapper toDtoMapper)
    : ApplicationService, IFinancialItemRankAmountAppService
{
    public async Task<FinancialItemRankAmountDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        var dto = toDtoMapper.Map(entity);
        await EnrichAsync(dto, entity);
        return dto;
    }

    public async Task<List<FinancialItemRankAmountDto>> GetByFinancialItemAsync(Guid financialItemId)
    {
        var queryable = await repository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(
            queryable.Where(x => x.FinancialItemId == financialItemId));

        var rankIds = items.Select(x => x.RankId).Distinct().ToList();
        var ranks = await employeeResolver.GetAllRanksAsync();
        var rankLookup = ranks.ToDictionary(r => r.Id);

        return items.Select(e =>
        {
            var dto = toDtoMapper.Map(e);
            if (rankLookup.TryGetValue(e.RankId, out var rank))
            {
                dto.RankNameAr = rank.NameAr;
                dto.RankNameEn = rank.NameEn;
            }
            return dto;
        }).ToList();
    }

    [Authorize(TrainingPermissions.FinancialItemRankAmount.Create)]
    public async Task<FinancialItemRankAmountDto> CreateAsync(CreateUpdateFinancialItemRankAmountDto input)
    {
        var entity = new FinancialItemRankAmount(
            GuidGenerator.Create(), input.FinancialItemId, input.RankId, input.AmountOMR);
        await repository.InsertAsync(entity, autoSave: true);
        var dto = toDtoMapper.Map(entity);
        await EnrichAsync(dto, entity);
        return dto;
    }

    [Authorize(TrainingPermissions.FinancialItemRankAmount.Update)]
    public async Task<FinancialItemRankAmountDto> UpdateAsync(Guid id, CreateUpdateFinancialItemRankAmountDto input)
    {
        var entity = await repository.GetAsync(id);
        entity.FinancialItemId = input.FinancialItemId;
        entity.RankId = input.RankId;
        entity.AmountOMR = input.AmountOMR;
        await repository.UpdateAsync(entity, autoSave: true);
        var dto = toDtoMapper.Map(entity);
        await EnrichAsync(dto, entity);
        return dto;
    }

    [Authorize(TrainingPermissions.FinancialItemRankAmount.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await repository.DeleteAsync(id);
    }

    private async Task EnrichAsync(FinancialItemRankAmountDto dto, FinancialItemRankAmount entity)
    {
        var ranks = await employeeResolver.GetAllRanksAsync();
        var rank = ranks.FirstOrDefault(r => r.Id == entity.RankId);
        if (rank != null)
        {
            dto.RankNameAr = rank.NameAr;
            dto.RankNameEn = rank.NameEn;
        }
    }
}
