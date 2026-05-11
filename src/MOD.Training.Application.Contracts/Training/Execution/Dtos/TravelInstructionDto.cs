using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Execution.Dtos;

public class TravelInstructionDto : FullAuditedEntityDto<Guid>
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }

    public DateTime DepartureDate { get; set; }
    public DateTime ArrivalDate { get; set; }
    public DateTime ReturnDate { get; set; }
    public DateTime ArrivalBackDate { get; set; }

    public bool VisaRequired { get; set; }
    public string? VisaNotes { get; set; }
    public bool InsuranceArranged { get; set; }
    public string? InsuranceProvider { get; set; }
    public bool TicketsBooked { get; set; }
    public string? TicketReference { get; set; }

    public int CalculatedTravelDays { get; set; }
    public int? OverrideTravelDays { get; set; }
    public int EffectiveTravelDays { get; set; }   // OverrideTravelDays ?? CalculatedTravelDays

    public TravelInstructionStatus Status { get; set; }
}
