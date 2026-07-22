using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Travel.Localization;
using Travel.Permissions;
using Travel.Ranks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace Travel.Allowances;

[Authorize(TravelManagementPermissions.ClothingAllowanceRules.Default)]
public class ClothingAllowanceRuleAppService :
    CrudAppService<
        ClothingAllowanceRule,
        ClothingAllowanceRuleDto,
        Guid,
        GetClothingAllowanceRuleListInput,
        CreateUpdateClothingAllowanceRuleDto>,
    IClothingAllowanceRuleAppService
{
    private readonly ITravelRankLookupAppService _rankLookupAppService;

    public ClothingAllowanceRuleAppService(
        IRepository<ClothingAllowanceRule, Guid> repository,
        ITravelRankLookupAppService rankLookupAppService) : base(repository)
    {
        _rankLookupAppService = rankLookupAppService;

        ObjectMapperContext = typeof(TravelApplicationModule);
        LocalizationResource = typeof(TravelResource);
        GetPolicyName = TravelManagementPermissions.ClothingAllowanceRules.Default;
        GetListPolicyName = TravelManagementPermissions.ClothingAllowanceRules.Default;
        CreatePolicyName = TravelManagementPermissions.ClothingAllowanceRules.Create;
        UpdatePolicyName = TravelManagementPermissions.ClothingAllowanceRules.Edit;
        DeletePolicyName = TravelManagementPermissions.ClothingAllowanceRules.Delete;
    }

    protected override async Task<IQueryable<ClothingAllowanceRule>> CreateFilteredQueryAsync(GetClothingAllowanceRuleListInput input)
    {
        var query = await Repository.WithDetailsAsync(x => x.Ranks);

        query = query
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), x => x.Name.Contains(input.Filter!))
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive!.Value);

        return query;
    }

    protected override async Task<ClothingAllowanceRule> MapToEntityAsync(CreateUpdateClothingAllowanceRuleDto createInput)
    {
        return new ClothingAllowanceRule(
            Guid.NewGuid(),
            createInput.Name,
            createInput.FullAmount,
            createInput.AnnualPartialAmount,
            createInput.FullPaymentPeriodYears,
            createInput.Priority,
            createInput.IsActive,
            createInput.RankIds);
    }

    protected override async Task MapToEntityAsync(CreateUpdateClothingAllowanceRuleDto updateInput, ClothingAllowanceRule entity)
    {
        entity.Update(
            updateInput.Name,
            updateInput.FullAmount,
            updateInput.AnnualPartialAmount,
            updateInput.FullPaymentPeriodYears,
            updateInput.Priority,
            updateInput.IsActive);

        entity.SetRanks(updateInput.RankIds);
    }

    protected override async Task<ClothingAllowanceRule> GetEntityByIdAsync(Guid id)
    {
        var query = await Repository.WithDetailsAsync(x => x.Ranks);
        var entity = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id));

        return entity ?? throw new EntityNotFoundException(typeof(ClothingAllowanceRule), id);
    }

    public override async Task<PagedResultDto<ClothingAllowanceRuleDto>> GetListAsync(GetClothingAllowanceRuleListInput input)
    {
        var result = await base.GetListAsync(input);
        await EnrichRankNamesAsync(result.Items);
        return result;
    }

    public override async Task<ClothingAllowanceRuleDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await EnrichRankNamesAsync([dto]);
        return dto;
    }

    protected override ClothingAllowanceRuleDto MapToGetOutputDto(ClothingAllowanceRule entity)
    {
        var dto = base.MapToGetOutputDto(entity);
        dto.RankIds = entity.Ranks.Select(x => x.RankId).ToList();
        return dto;
    }

    protected override ClothingAllowanceRuleDto MapToGetListOutputDto(ClothingAllowanceRule entity)
    {
        var dto = base.MapToGetListOutputDto(entity);
        dto.RankIds = entity.Ranks.Select(x => x.RankId).ToList();
        return dto;
    }

    private async Task EnrichRankNamesAsync(IReadOnlyList<ClothingAllowanceRuleDto> items)
    {
        var ranks = await _rankLookupAppService.GetRanksAsync();
        var rankDict = ranks.ToDictionary(x => x.Id, x => x.Name);

        foreach (var item in items)
        {
            item.RankNames = item.RankIds
                .Select(id => rankDict.GetValueOrDefault(id))
                .Where(name => !name.IsNullOrWhiteSpace())
                .Select(name => name!)
                .ToList();
        }
    }
}
