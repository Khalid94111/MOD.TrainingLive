using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Execution;

/// <summary>
/// Travel package for a casual course or annual-plan session — one row per polymorphic parent.
/// Holds the four travel dates, visa / insurance / ticket state, and computed travel days.
/// Phase 4B-α writes; Phase 4B-β reads (TravelAllowancePayment consumes EffectiveTravelDays).
/// </summary>
public class TravelInstruction : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }

    public DateTime DepartureDate { get; set; }       // Leaving Oman
    public DateTime ArrivalDate { get; set; }         // Arriving at host country
    public DateTime ReturnDate { get; set; }          // Leaving host country
    public DateTime ArrivalBackDate { get; set; }     // Arriving back in Oman

    public bool VisaRequired { get; set; }
    public string? VisaNotes { get; set; }
    public bool InsuranceArranged { get; set; }
    public string? InsuranceProvider { get; set; }
    public bool TicketsBooked { get; set; }
    public string? TicketReference { get; set; }

    public int CalculatedTravelDays { get; set; }    // Server-computed
    public int? OverrideTravelDays { get; set; }     // Staff override

    public TravelInstructionStatus Status { get; set; }

    protected TravelInstruction() { }

    public TravelInstruction(Guid id) : base(id)
    {
        Status = TravelInstructionStatus.Draft;
    }
}
