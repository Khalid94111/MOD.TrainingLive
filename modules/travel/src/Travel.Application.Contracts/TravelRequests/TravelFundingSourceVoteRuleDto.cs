using System;
using Volo.Abp.Application.Dtos;

namespace Travel.TravelRequests;

public class TravelFundingSourceVoteRuleDto : FullAuditedEntityDto<Guid>
{
    public string PaymentType { get; set; } = string.Empty;
    public string FundingSourceVoteCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
