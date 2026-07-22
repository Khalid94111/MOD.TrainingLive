namespace Travel.TravelRequests;

public class TrainingTravelPaymentDto
{
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FundingSourceVoteCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
