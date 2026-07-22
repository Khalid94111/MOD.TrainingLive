namespace Travel.TravelRequests;

public class TravelCostSummaryDto
{
    public int EmployeeCount { get; set; }
    public decimal TicketCostPerEmployee { get; set; }
    public decimal TicketsTotal { get; set; }
    public decimal VisaCostPerEmployee { get; set; }
    public decimal VisaTotal { get; set; }
    public decimal TravelInsuranceCostPerEmployee { get; set; }
    public decimal TravelInsuranceTotal { get; set; }
    public decimal AllowancesTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public bool IsAllowanceCalculated { get; set; }
}
