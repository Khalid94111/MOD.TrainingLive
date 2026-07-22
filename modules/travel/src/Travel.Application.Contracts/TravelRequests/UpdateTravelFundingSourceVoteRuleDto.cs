using System.ComponentModel.DataAnnotations;

namespace Travel.TravelRequests;

public class UpdateTravelFundingSourceVoteRuleDto
{
    [Required]
    [StringLength(TravelRequestConsts.MaxFundingSourceVoteCodeLength)]
    public string FundingSourceVoteCode { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
