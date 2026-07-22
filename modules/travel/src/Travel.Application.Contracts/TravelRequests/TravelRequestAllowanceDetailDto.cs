using System;
using System.Collections.Generic;
using Travel.Allowances;

namespace Travel.TravelRequests;

public class TravelRequestAllowanceDetailDto
{
    public Guid TravelRequestId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string RankName { get; set; } = string.Empty;
    public AllowanceCategory Category { get; set; }
    public decimal DailyRate { get; set; }
    public int CalculatedDays { get; set; }
    public decimal AccommodationPaymentPercentage { get; set; }
    public decimal OverseasTotal { get; set; }
    public decimal ClothingAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal NetTotal { get; set; }
    public bool HasMatchingRule { get; set; }
    public List<AllowanceCalculationSegment> Segments { get; set; } = new();
    public string ClothingCalculationNote { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
}
