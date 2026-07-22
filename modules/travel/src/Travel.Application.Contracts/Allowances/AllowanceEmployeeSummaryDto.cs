using System;
using System.Collections.Generic;

namespace Travel.Allowances;

public class AllowanceEmployeeSummaryDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string RankName { get; set; } = string.Empty;
    public AllowanceCategory Category { get; set; }
    public decimal DailyRate { get; set; }
    public decimal AccommodationPaymentPercentage { get; set; } = 100m;
    public decimal OverseasTotal { get; set; }
    public decimal ClothingAmount { get; set; }
    public bool ClothingEligible { get; set; }
    public string ClothingCalculationNote { get; set; } = string.Empty;
    public decimal DeductionAmount { get; set; }
    public decimal NetTotal { get; set; }
    public bool HasMatchingRule { get; set; }
    public List<AllowanceCalculationSegment> Segments { get; set; } = new();
}
