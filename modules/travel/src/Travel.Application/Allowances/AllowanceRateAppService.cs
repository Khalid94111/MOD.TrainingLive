using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Travel.Localization;
using Travel.Permissions;
using Travel.Ranks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Travel.Allowances;

[Authorize(TravelManagementPermissions.AllowanceRates.Default)]
public class AllowanceRateAppService :
    CrudAppService<
        AllowanceRate,
        AllowanceRateDto,
        Guid,
        GetAllowanceRateListInput,
        CreateUpdateAllowanceRateDto>,
    IAllowanceRateAppService
{
    private readonly ITravelRankLookupAppService _rankLookupAppService;

    public AllowanceRateAppService(
        IRepository<AllowanceRate, Guid> repository,
        ITravelRankLookupAppService rankLookupAppService) : base(repository)
    {
        _rankLookupAppService = rankLookupAppService;

        ObjectMapperContext = typeof(TravelApplicationModule);
        LocalizationResource = typeof(TravelResource);
        GetPolicyName = TravelManagementPermissions.AllowanceRates.Default;
        GetListPolicyName = TravelManagementPermissions.AllowanceRates.Default;
        CreatePolicyName = TravelManagementPermissions.AllowanceRates.Create;
        UpdatePolicyName = TravelManagementPermissions.AllowanceRates.Edit;
        DeletePolicyName = TravelManagementPermissions.AllowanceRates.Delete;
    }

    protected override async Task<IQueryable<AllowanceRate>> CreateFilteredQueryAsync(GetAllowanceRateListInput input)
    {
        var query = await Repository.GetQueryableAsync();

        query = query
            .WhereIf(input.RankId.HasValue, x => x.RankId == input.RankId!.Value)
            .WhereIf(input.Category.HasValue, x => x.Category == input.Category!.Value)
            .WhereIf(input.AllowanceType.HasValue, x => x.AllowanceType == input.AllowanceType!.Value);

        return query;
    }

    protected override async Task<AllowanceRate> MapToEntityAsync(CreateUpdateAllowanceRateDto createInput)
    {
        EnsureDailyAllowanceRate(createInput.AllowanceType);

        return new AllowanceRate(
            Guid.NewGuid(),
            createInput.RankId,
            createInput.Category,
            createInput.AllowanceType,
            createInput.Amount,
            createInput.TicketClass,
            NormalizeAnnualPartialAmount(createInput),
            createInput.IsActive);
    }

    protected override async Task MapToEntityAsync(CreateUpdateAllowanceRateDto updateInput, AllowanceRate entity)
    {
        EnsureDailyAllowanceRate(updateInput.AllowanceType);

        entity.Update(
            updateInput.RankId,
            updateInput.Category,
            updateInput.Amount,
            updateInput.TicketClass,
            NormalizeAnnualPartialAmount(updateInput),
            updateInput.IsActive);
    }

    public override async Task<PagedResultDto<AllowanceRateDto>> GetListAsync(GetAllowanceRateListInput input)
    {
        var result = await base.GetListAsync(input);
        var ranks = await _rankLookupAppService.GetRanksAsync();
        var rankDict = ranks.ToDictionary(r => r.Id, r => r.Name);

        foreach (var item in result.Items)
        {
            if (rankDict.TryGetValue(item.RankId, out var name))
            {
                item.RankName = name;
            }
        }

        return result;
    }

    public override async Task<AllowanceRateDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        var rank = await _rankLookupAppService.GetRankByIdAsync(dto.RankId);
        if (rank != null)
        {
            dto.RankName = rank.Name;
        }
        return dto;
    }

    private static decimal? NormalizeAnnualPartialAmount(CreateUpdateAllowanceRateDto input)
    {
        return null;
    }

    private static void EnsureDailyAllowanceRate(AllowanceType allowanceType)
    {
        if (allowanceType != AllowanceType.Daily)
        {
            throw new BusinessException(TravelErrorCodes.UseClothingAllowanceRules);
        }
    }
}
