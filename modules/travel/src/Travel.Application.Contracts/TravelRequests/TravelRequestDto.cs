using System;
using System.Collections.Generic;
using Travel.Allowances;
using Travel.TravelTypes;
using Volo.Abp.Application.Dtos;

namespace Travel.TravelRequests;

public class TravelRequestDto : FullAuditedEntityDto<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TravelTypeDefinitionId { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string DestinationCountry { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public AllowanceCategory Category { get; set; }
    public decimal DailyAllowanceRate { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool NeedsPermission { get; set; }
    public bool NeedsAwareness { get; set; }
    public bool HasTicketCompensation { get; set; }
    public bool NeedsTransportation { get; set; }
    public bool IncludesAccommodation { get; set; }
    public bool UseHighestAllowance { get; set; }
    public string AllowanceTiers { get; set; } = string.Empty;
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public bool RequiresVisa { get; set; }
    public decimal VisaCostPerEmployee { get; set; }
    public bool RequiresTravelInsurance { get; set; }
    public decimal TravelInsuranceCostPerEmployee { get; set; }
    public string TicketAirline { get; set; } = string.Empty;
    public string TicketFlightNumber { get; set; } = string.Empty;
    public DateTime? TicketDepartureTime { get; set; }
    public DateTime? TicketArrivalTime { get; set; }
    public decimal TicketCostPerEmployee { get; set; }
    public string TravelOfficeNotes { get; set; } = string.Empty;
    public Guid? SelectedDepartureFlightOfferId { get; set; }
    public Guid? SelectedReturnFlightOfferId { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public Guid? SourceTenantId { get; set; }
    public Guid? SourceTrainingCourseId { get; set; }
    public string SourceTrainingCourseName { get; set; } = string.Empty;
    public string FundingSourceVoteCode { get; set; } = string.Empty;
    public string TicketFundingSourceVoteCode { get; set; } = string.Empty;
    public string VisaFundingSourceVoteCode { get; set; } = string.Empty;
    public string HealthInsuranceFundingSourceVoteCode { get; set; } = string.Empty;
    public string DailyAllowanceFundingSourceVoteCode { get; set; } = string.Empty;
    public string ClothingAllowanceFundingSourceVoteCode { get; set; } = string.Empty;
    public string IntegrationWarnings { get; set; } = string.Empty;
    public List<TravelRequestEmployeeDto> Employees { get; set; } = new();
    public List<TravelDocumentDto> Documents { get; set; } = new();
    public List<TravelFlightOfferDto> FlightOffers { get; set; } = new();
    public List<TravelEmployeeDocumentDto> EmployeeDocuments { get; set; } = new();
    public List<TravelRequestAllowanceDetailDto> AllowanceDetails { get; set; } = new();
    public AllowanceSnapshotDto? AllowanceSnapshot { get; set; }
}
