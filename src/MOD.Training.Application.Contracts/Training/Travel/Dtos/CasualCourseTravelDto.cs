using System;
using System.Collections.Generic;

namespace MOD.Training.Training.Travel.Dtos;

/// <summary>
/// Training-owned read model for the Travel process linked to an ad-hoc course.
/// </summary>
public class CasualCourseTravelDto
{
    public Guid CasualCourseId { get; set; }
    public Guid? TravelRequestId { get; set; }

    public bool IntegrationAvailable { get; set; }
    public bool IsFound { get; set; }
    public bool IsCompleted { get; set; }
    public bool CanSend { get; set; }

    public string StatusCode { get; set; } = string.Empty;
    public string WarningMessage { get; set; } = string.Empty;
    public List<string> BlockingReasons { get; set; } = [];

    public DateTime? CompletedAt { get; set; }
    public int CalculatedDays { get; set; }
    public int EmployeeCount { get; set; }
    public string Currency { get; set; } = "OMR";
    public decimal TicketsTotal { get; set; }
    public decimal VisaTotal { get; set; }
    public decimal HealthInsuranceTotal { get; set; }
    public decimal TravelAllowanceTotal { get; set; }
    public decimal ClothingAllowanceTotal { get; set; }
    public decimal DeductionTotal { get; set; }
    public decimal GrandTotal { get; set; }
}
