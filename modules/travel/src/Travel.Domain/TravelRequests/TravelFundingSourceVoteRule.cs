using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Travel.TravelRequests;

public class TravelFundingSourceVoteRule : FullAuditedAggregateRoot<Guid>
{
    public string PaymentType { get; private set; } = string.Empty;
    public string FundingSourceVoteCode { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    protected TravelFundingSourceVoteRule()
    {
    }

    public TravelFundingSourceVoteRule(
        Guid id,
        string paymentType,
        string fundingSourceVoteCode,
        bool isActive = true) : base(id)
    {
        PaymentType = paymentType ?? string.Empty;
        FundingSourceVoteCode = fundingSourceVoteCode ?? string.Empty;
        IsActive = isActive;
    }

    public void Update(string fundingSourceVoteCode, bool isActive)
    {
        FundingSourceVoteCode = fundingSourceVoteCode ?? string.Empty;
        IsActive = isActive;
    }
}
