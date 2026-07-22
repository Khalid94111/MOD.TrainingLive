using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Travel.TravelRequests;

public class CreateTravelRequestFromTrainingDto
{
    public Guid TrainingCourseId { get; set; }

    [Required]
    [StringLength(TravelRequestConsts.MaxTitleLength)]
    public string CourseName { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    [StringLength(TravelRequestConsts.MaxCountryLength)]
    public string DestinationCountry { get; set; } = string.Empty;

    [Required]
    [StringLength(TravelRequestConsts.MaxCityLength)]
    public string DestinationCity { get; set; } = string.Empty;

    [Required]
    public Guid? TenantId { get; set; }

    [Required]
    [StringLength(TravelRequestConsts.MaxFundingSourceVoteCodeLength)]
    public string TicketFundingSourceVoteCode { get; set; } = string.Empty;

    [Required]
    [StringLength(TravelRequestConsts.MaxFundingSourceVoteCodeLength)]
    public string VisaFundingSourceVoteCode { get; set; } = string.Empty;

    [Required]
    [StringLength(TravelRequestConsts.MaxFundingSourceVoteCodeLength)]
    public string HealthInsuranceFundingSourceVoteCode { get; set; } = string.Empty;

    [Required]
    [StringLength(TravelRequestConsts.MaxFundingSourceVoteCodeLength)]
    public string DailyAllowanceFundingSourceVoteCode { get; set; } = string.Empty;

    [Required]
    [StringLength(TravelRequestConsts.MaxFundingSourceVoteCodeLength)]
    public string ClothingAllowanceFundingSourceVoteCode { get; set; } = string.Empty;

    public List<string> EmployeeNumbers { get; set; } = new();
}
