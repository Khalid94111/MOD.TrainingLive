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

[Authorize(TravelManagementPermissions.AllowanceRules.Default)]
public class AllowanceRuleAppService :
    CrudAppService<
        AllowanceRule,
        AllowanceRuleDto,
        Guid,
        GetAllowanceRuleListInput,
        CreateUpdateAllowanceRuleDto>,
    IAllowanceRuleAppService
{
    private readonly IRepository<AllowanceRuleSegment, Guid> _segmentRepository;

    public AllowanceRuleAppService(
        IRepository<AllowanceRule, Guid> repository,
        IRepository<AllowanceRuleSegment, Guid> segmentRepository) : base(repository)
    {
        _segmentRepository = segmentRepository;
        ObjectMapperContext = typeof(TravelApplicationModule);
        LocalizationResource = typeof(TravelResource);
        GetPolicyName = TravelManagementPermissions.AllowanceRules.Default;
        GetListPolicyName = TravelManagementPermissions.AllowanceRules.Default;
        CreatePolicyName = TravelManagementPermissions.AllowanceRules.Create;
        UpdatePolicyName = TravelManagementPermissions.AllowanceRules.Edit;
        DeletePolicyName = TravelManagementPermissions.AllowanceRules.Delete;
    }

    protected override async Task<IQueryable<AllowanceRule>> CreateFilteredQueryAsync(GetAllowanceRuleListInput input)
    {
        var query = await Repository.GetQueryableAsync();

        query = query
            .WhereIf(input.AllowanceType.HasValue, x => x.AllowanceType == input.AllowanceType!.Value)
            .WhereIf(input.TravelTypeDefinitionId.HasValue, x => x.TravelTypeDefinitionId == input.TravelTypeDefinitionId!.Value)
            .WhereIf(input.Category.HasValue, x => x.Category == input.Category!.Value)
            .WhereIf(input.AppliesWhenAccommodationIncluded.HasValue, x => x.AppliesWhenAccommodationIncluded == input.AppliesWhenAccommodationIncluded!.Value)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(),
                x => x.Name.Contains(input.Filter!));

        return query;
    }

    public override async Task<PagedResultDto<AllowanceRuleDto>> GetListAsync(GetAllowanceRuleListInput input)
    {
        var result = await base.GetListAsync(input);
        await FillSegmentsAsync(result.Items.ToList());
        return result;
    }

    public override async Task<AllowanceRuleDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await FillSegmentsAsync([dto]);
        return dto;
    }

    public virtual async Task<AllowanceRuleTestResultDto> TestAsync(Guid id, AllowanceRuleTestInputDto input)
    {
        await CheckGetPolicyAsync();

        if (input.Days <= 0 || input.DailyRate < 0 || input.EmployeeCount <= 0)
        {
            throw new UserFriendlyException(L["InvalidRuleTestInput"]);
        }

        var rule = await Repository.GetAsync(id);
        var ruleSegments = await _segmentRepository.GetListAsync(x => x.AllowanceRuleId == rule.Id);
        var segments = CalculateSegments(
            input.Days,
            input.DailyRate,
            Math.Clamp(input.AccommodationPaymentPercentage, 0m, 100m),
            ruleSegments
                .Where(x => x.AppliesToTotalDays(input.Days))
                .OrderBy(x => x.FromDay)
                .ToList());

        var totalPerEmployee = segments.Sum(x => x.Amount);

        return new AllowanceRuleTestResultDto
        {
            Days = input.Days,
            DailyRate = input.DailyRate,
            EmployeeCount = input.EmployeeCount,
            AccommodationPaymentPercentage = Math.Clamp(input.AccommodationPaymentPercentage, 0m, 100m),
            TotalPerEmployee = totalPerEmployee,
            GrandTotal = totalPerEmployee * input.EmployeeCount,
            Segments = segments
        };
    }

    protected override async Task<AllowanceRule> MapToEntityAsync(CreateUpdateAllowanceRuleDto createInput)
    {
        var rule = new AllowanceRule(
            Guid.NewGuid(),
            createInput.Name,
            createInput.AllowanceType,
            createInput.TravelTypeDefinitionId,
            createInput.Category,
            createInput.AppliesWhenAccommodationIncluded,
            createInput.AccommodationMultiplier,
            createInput.Priority,
            createInput.IsActive);

        foreach (var segment in createInput.Segments.OrderBy(s => s.FromDay))
        {
            rule.AddSegment(
                segment.FromDay,
                segment.ToDay,
                segment.Percentage,
                segment.AppliesWhenTotalDaysFrom,
                segment.AppliesWhenTotalDaysTo);
        }

        return rule;
    }

    protected override async Task MapToEntityAsync(CreateUpdateAllowanceRuleDto updateInput, AllowanceRule entity)
    {
        entity.Update(
            updateInput.Name,
            updateInput.AllowanceType,
            updateInput.TravelTypeDefinitionId,
            updateInput.Category,
            updateInput.AppliesWhenAccommodationIncluded,
            updateInput.AccommodationMultiplier,
            updateInput.Priority,
            updateInput.IsActive);

        await _segmentRepository.DeleteAsync(x => x.AllowanceRuleId == entity.Id);
        entity.ClearSegments();
        foreach (var segment in updateInput.Segments.OrderBy(s => s.FromDay))
        {
            entity.AddSegment(
                segment.FromDay,
                segment.ToDay,
                segment.Percentage,
                segment.AppliesWhenTotalDaysFrom,
                segment.AppliesWhenTotalDaysTo);
        }
    }

    private async Task FillSegmentsAsync(List<AllowanceRuleDto> rules)
    {
        if (rules.Count == 0)
        {
            return;
        }

        var ruleIds = rules.Select(x => x.Id).ToList();
        var segments = await _segmentRepository.GetListAsync(x => ruleIds.Contains(x.AllowanceRuleId));
        var segmentLookup = segments
            .GroupBy(x => x.AllowanceRuleId)
            .ToDictionary(x => x.Key, x => x.OrderBy(s => s.FromDay).ToList());

        foreach (var rule in rules)
        {
            rule.Segments = segmentLookup.TryGetValue(rule.Id, out var ruleSegments)
                ? ruleSegments.Select(s => new AllowanceRuleSegmentDto
                {
                    Id = s.Id,
                    AllowanceRuleId = s.AllowanceRuleId,
                    FromDay = s.FromDay,
                    ToDay = s.ToDay,
                    Percentage = s.Percentage,
                    AppliesWhenTotalDaysFrom = s.AppliesWhenTotalDaysFrom,
                    AppliesWhenTotalDaysTo = s.AppliesWhenTotalDaysTo
                }).ToList()
                : new List<AllowanceRuleSegmentDto>();
        }
    }

    private static List<AllowanceCalculationSegment> CalculateSegments(
        int days,
        decimal dailyRate,
        decimal accommodationPaymentPercentage,
        List<AllowanceRuleSegment> ruleSegments)
    {
        var result = new List<AllowanceCalculationSegment>();
        var multiplier = accommodationPaymentPercentage / 100m;

        if (!ruleSegments.Any())
        {
            result.Add(new AllowanceCalculationSegment(1, days, days, 100m * multiplier, days * dailyRate * multiplier));
            return result;
        }

        foreach (var segment in ruleSegments)
        {
            var overlapFrom = Math.Max(segment.FromDay, 1);
            var overlapTo = Math.Min(segment.ToDay, days);

            if (overlapFrom > overlapTo)
            {
                continue;
            }

            var segmentDays = overlapTo - overlapFrom + 1;
            var effectivePercentage = segment.Percentage * multiplier;
            result.Add(new AllowanceCalculationSegment(
                overlapFrom,
                overlapTo,
                segmentDays,
                effectivePercentage,
                segmentDays * dailyRate * (effectivePercentage / 100m)));
        }

        var lastSegmentEnd = ruleSegments.Max(s => s.ToDay);
        if (days > lastSegmentEnd)
        {
            var extraDays = days - lastSegmentEnd;
            result.Add(new AllowanceCalculationSegment(
                lastSegmentEnd + 1,
                days,
                extraDays,
                100m * multiplier,
                extraDays * dailyRate * multiplier));
        }

        return result;
    }
}
