using System.Collections.Generic;

namespace Travel.Allowances;

public class AllowanceRuleTestResultDto
{
    public int Days { get; set; }
    public decimal DailyRate { get; set; }
    public int EmployeeCount { get; set; }
    public decimal AccommodationPaymentPercentage { get; set; }
    public decimal TotalPerEmployee { get; set; }
    public decimal GrandTotal { get; set; }
    public List<AllowanceCalculationSegment> Segments { get; set; } = new();
}
