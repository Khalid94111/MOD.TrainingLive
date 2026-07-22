using System;
using System.Collections.Generic;

namespace Travel.Allowances;

public class AllowanceSummaryDto
{
    public int TotalDays { get; set; }
    public decimal DailyBaseRate { get; set; }
    public decimal? AccommodationMultiplier { get; set; }
    public decimal OverseasTotal { get; set; }
    public List<AllowanceCalculationSegment> OverseasSegments { get; set; } = new();
    public List<AllowanceEmployeeSummaryDto> EmployeeAllowances { get; set; } = new();
    public decimal ClothingAmount { get; set; }
    public bool ClothingEligible { get; set; }
    public string ClothingCalculationNote { get; set; } = string.Empty;
    public decimal DeductionAmount { get; set; }
    public decimal NetTotal { get; set; }
    public DateTime CalculatedAt { get; set; }
    public List<string> Warnings { get; set; } = new();
}
