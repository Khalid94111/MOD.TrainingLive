using System;
using System.Collections.Generic;

namespace Travel.TravelRequests;

public class OverseasPayrollPreviewDto
{
    public int TotalDays { get; set; }

    public int ExtraDays { get; set; }

    public DateTime ExtraPeriodStartDate { get; set; }

    public DateTime ExtraPeriodEndDate { get; set; }

    public bool RequiresPayrollElementSelection { get; set; }

    public List<OverseasPayrollEmployeePreviewDto> Employees { get; set; } = new();
}

public class OverseasPayrollEmployeePreviewDto
{
    public Guid EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public string EmployeeNumber { get; set; } = string.Empty;

    public string RankName { get; set; } = string.Empty;

    public decimal DailyRate { get; set; }

    public decimal Percentage { get; set; }

    public decimal AccommodationPaymentPercentage { get; set; }

    public decimal DailyAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public int ExtraDays { get; set; }
}
