using System;
using System.Collections.Generic;

namespace Travel.TravelRequests;

public class TrainingCourseLookupDto
{
    public Guid TrainingCourseId { get; set; }
    public Guid? TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string DestinationCountry { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public string TicketFundingSourceVoteCode { get; set; } = string.Empty;
    public string VisaFundingSourceVoteCode { get; set; } = string.Empty;
    public string HealthInsuranceFundingSourceVoteCode { get; set; } = string.Empty;
    public string DailyAllowanceFundingSourceVoteCode { get; set; } = string.Empty;
    public string ClothingAllowanceFundingSourceVoteCode { get; set; } = string.Empty;
    public List<string> EmployeeNumbers { get; set; } = new();
}
