using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Travel.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Travel.TravelRequests;

[Authorize(TravelManagementPermissions.TravelRequests.ManageAllowances)]
public class TravelFundingSourceVoteRuleAppService :
    ApplicationService,
    ITravelFundingSourceVoteRuleAppService
{
    private readonly IRepository<TravelFundingSourceVoteRule, Guid> _repository;

    public TravelFundingSourceVoteRuleAppService(IRepository<TravelFundingSourceVoteRule, Guid> repository)
    {
        _repository = repository;
    }

    public virtual async Task<List<TravelFundingSourceVoteRuleDto>> GetListAsync()
    {
        var rules = await _repository.GetListAsync();

        return rules
            .OrderBy(rule => GetPaymentTypeOrder(rule.PaymentType))
            .ThenBy(rule => rule.PaymentType)
            .Select(MapToDto)
            .ToList();
    }

    [Authorize(TravelManagementPermissions.TravelRequests.ManageAllowances)]
    public virtual async Task<TravelFundingSourceVoteRuleDto> UpdateAsync(Guid id, UpdateTravelFundingSourceVoteRuleDto input)
    {
        var rule = await _repository.GetAsync(id);

        rule.Update(input.FundingSourceVoteCode, input.IsActive);
        await _repository.UpdateAsync(rule);

        return MapToDto(rule);
    }

    private static TravelFundingSourceVoteRuleDto MapToDto(TravelFundingSourceVoteRule rule)
    {
        return new TravelFundingSourceVoteRuleDto
        {
            Id = rule.Id,
            PaymentType = rule.PaymentType,
            FundingSourceVoteCode = rule.FundingSourceVoteCode,
            IsActive = rule.IsActive,
            CreationTime = rule.CreationTime,
            CreatorId = rule.CreatorId,
            LastModificationTime = rule.LastModificationTime,
            LastModifierId = rule.LastModifierId
        };
    }

    private static int GetPaymentTypeOrder(string paymentType)
    {
        return paymentType switch
        {
            FundingSourceVotePaymentType.Ticket => 1,
            FundingSourceVotePaymentType.Visa => 2,
            FundingSourceVotePaymentType.HealthInsurance => 3,
            FundingSourceVotePaymentType.DailyAllowance => 4,
            FundingSourceVotePaymentType.ClothingAllowance => 5,
            _ => 99
        };
    }
}
