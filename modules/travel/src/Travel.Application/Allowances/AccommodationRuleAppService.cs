using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Travel.Localization;
using Travel.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Travel.Allowances;

[Authorize(TravelManagementPermissions.AccommodationRules.Default)]
public class AccommodationRuleAppService :
    CrudAppService<
        AccommodationRule,
        AccommodationRuleDto,
        Guid,
        GetAccommodationRuleListInput,
        CreateUpdateAccommodationRuleDto>,
    IAccommodationRuleAppService
{
    private readonly IRepository<AccommodationRuleAllowanceRule, Guid> _linkRepository;
    private readonly IRepository<AllowanceRule, Guid> _allowanceRuleRepository;

    public AccommodationRuleAppService(
        IRepository<AccommodationRule, Guid> repository,
        IRepository<AccommodationRuleAllowanceRule, Guid> linkRepository,
        IRepository<AllowanceRule, Guid> allowanceRuleRepository) : base(repository)
    {
        _linkRepository = linkRepository;
        _allowanceRuleRepository = allowanceRuleRepository;
        ObjectMapperContext = typeof(TravelApplicationModule);
        LocalizationResource = typeof(TravelResource);
        GetPolicyName = TravelManagementPermissions.AccommodationRules.Default;
        GetListPolicyName = TravelManagementPermissions.AccommodationRules.Default;
        CreatePolicyName = TravelManagementPermissions.AccommodationRules.Create;
        UpdatePolicyName = TravelManagementPermissions.AccommodationRules.Edit;
        DeletePolicyName = TravelManagementPermissions.AccommodationRules.Delete;
    }

    protected override async Task<IQueryable<AccommodationRule>> CreateFilteredQueryAsync(GetAccommodationRuleListInput input)
    {
        var query = await Repository.GetQueryableAsync();

        return query
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), x => x.Name.Contains(input.Filter!));
    }

    public override async Task<PagedResultDto<AccommodationRuleDto>> GetListAsync(GetAccommodationRuleListInput input)
    {
        var result = await base.GetListAsync(input);
        await FillAllowanceRuleLinksAsync(result.Items.ToList());
        return result;
    }

    public override async Task<AccommodationRuleDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await FillAllowanceRuleLinksAsync([dto]);
        return dto;
    }

    protected override Task<AccommodationRule> MapToEntityAsync(CreateUpdateAccommodationRuleDto createInput)
    {
        return Task.FromResult(new AccommodationRule(
            Guid.NewGuid(),
            createInput.Name,
            createInput.PaymentPercentage,
            createInput.Priority,
            createInput.IsActive));
    }

    public override async Task<AccommodationRuleDto> CreateAsync(CreateUpdateAccommodationRuleDto input)
    {
        var dto = await base.CreateAsync(input);
        await ReplaceAllowanceRuleLinksAsync(dto.Id, input.AllowanceRuleIds);
        dto.AllowanceRuleIds = input.AllowanceRuleIds.Distinct().ToList();
        await FillAllowanceRuleLinksAsync([dto]);
        return dto;
    }

    public override async Task<AccommodationRuleDto> UpdateAsync(Guid id, CreateUpdateAccommodationRuleDto input)
    {
        var dto = await base.UpdateAsync(id, input);
        await ReplaceAllowanceRuleLinksAsync(id, input.AllowanceRuleIds);
        dto.AllowanceRuleIds = input.AllowanceRuleIds.Distinct().ToList();
        await FillAllowanceRuleLinksAsync([dto]);
        return dto;
    }

    protected override Task MapToEntityAsync(CreateUpdateAccommodationRuleDto updateInput, AccommodationRule entity)
    {
        entity.Update(
            updateInput.Name,
            updateInput.PaymentPercentage,
            updateInput.Priority,
            updateInput.IsActive);

        return Task.CompletedTask;
    }

    private async Task ReplaceAllowanceRuleLinksAsync(Guid accommodationRuleId, List<Guid> allowanceRuleIds)
    {
        await _linkRepository.DeleteAsync(x => x.AccommodationRuleId == accommodationRuleId);

        foreach (var allowanceRuleId in allowanceRuleIds.Distinct())
        {
            await _linkRepository.InsertAsync(new AccommodationRuleAllowanceRule(
                Guid.NewGuid(),
                accommodationRuleId,
                allowanceRuleId));
        }
    }

    private async Task FillAllowanceRuleLinksAsync(List<AccommodationRuleDto> rules)
    {
        if (rules.Count == 0)
        {
            return;
        }

        var ruleIds = rules.Select(x => x.Id).ToList();
        var links = await _linkRepository.GetListAsync(x => ruleIds.Contains(x.AccommodationRuleId));
        var allowanceRuleIds = links.Select(x => x.AllowanceRuleId).Distinct().ToList();
        var allowanceRules = allowanceRuleIds.Count == 0
            ? []
            : await _allowanceRuleRepository.GetListAsync(x => allowanceRuleIds.Contains(x.Id));
        var allowanceRuleNames = allowanceRules.ToDictionary(x => x.Id, x => x.Name);

        foreach (var rule in rules)
        {
            var ruleLinks = links.Where(x => x.AccommodationRuleId == rule.Id).ToList();
            rule.AllowanceRuleIds = ruleLinks.Select(x => x.AllowanceRuleId).ToList();
            rule.AllowanceRuleNames = ruleLinks
                .Select(x => allowanceRuleNames.TryGetValue(x.AllowanceRuleId, out var name) ? name : string.Empty)
                .Where(x => !x.IsNullOrWhiteSpace())
                .ToList();
        }
    }
}
