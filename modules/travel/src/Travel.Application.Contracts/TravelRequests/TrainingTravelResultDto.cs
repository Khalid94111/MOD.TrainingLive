using System;
using System.Collections.Generic;
using Travel.TravelTypes;

namespace Travel.TravelRequests;

public class TrainingTravelResultDto
{
    public Guid? TenantId { get; set; }
    public Guid TrainingCourseId { get; set; }
    public Guid? TravelRequestId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string TravelRequestTitle { get; set; } = string.Empty;
    public string FundingSourceVoteCode { get; set; } = string.Empty;
    public string TicketFundingSourceVoteCode { get; set; } = string.Empty;
    public string VisaFundingSourceVoteCode { get; set; } = string.Empty;
    public string HealthInsuranceFundingSourceVoteCode { get; set; } = string.Empty;
    public string DailyAllowanceFundingSourceVoteCode { get; set; } = string.Empty;
    public string ClothingAllowanceFundingSourceVoteCode { get; set; } = string.Empty;
    public DateTime? CourseStartDate { get; set; }
    public DateTime? CourseEndDate { get; set; }
    public string DestinationCountry { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public RequestStatus? Status { get; set; }
    public bool IsFound { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? TravelDepartureDate { get; set; }
    public DateTime? TravelReturnDate { get; set; }
    public DateTime? AllowanceStartDate { get; set; }
    public DateTime? AllowanceEndDate { get; set; }
    public int CalculatedDays { get; set; }
    public int EmployeeCount { get; set; }
    public string Currency { get; set; } = "OMR";
    public string WarningMessage { get; set; } = string.Empty;
    public decimal TicketsTotal { get; set; }
    public decimal VisaTotal { get; set; }
    public decimal HealthInsuranceTotal { get; set; }
    public decimal TravelAllowanceTotal { get; set; }
    public decimal ClothingAllowanceTotal { get; set; }
    public decimal DeductionTotal { get; set; }
    public decimal AllowancesTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public List<TrainingTravelEmployeeResultDto> Employees { get; set; } = new();
    public decimal TotalAmount { get; set; }
}
