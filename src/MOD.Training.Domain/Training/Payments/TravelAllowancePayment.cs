using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Payments;

/// <summary>
/// Per-nominee travel cost row for a casual course OR an annual-plan session.
/// Polymorphic parent: exactly one of CasualCourseId / SessionId is set (DB CHECK enforces).
/// TotalOMR is server-computed and never trusted from client. PersonnelType is auto-filled
/// from Nomination.Employee.Rank.PersonnelType. Phase 4B-β.
/// </summary>
public class TravelAllowancePayment : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    // Polymorphic parent — exactly one is set (DB CHECK constraint enforced).
    public Guid? SessionId { get; set; }
    public Guid? CasualCourseId { get; set; }

    public Guid NominationId { get; set; }            // FK — CasualCourseNomination.Id (casual arm) OR SessionNomination.Id (session arm)
    public PersonnelType PersonnelType { get; set; }  // auto-filled from nominee rank, never editable

    // Five amount components — all in OMR.
    public decimal TicketAmountOMR { get; set; }
    public decimal TravelAllowanceOMR { get; set; }
    public decimal ClothingAllowanceOMR { get; set; }
    public decimal InsuranceOMR { get; set; }
    public decimal VisaFeesOMR { get; set; }
    public decimal TotalOMR { get; set; }             // server-computed; never trusted from client

    public PaymentStatus Status { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedById { get; set; }

    // Future Nebras integration fields — read-only display, never user-editable.
    public string? ExternalRequestId { get; set; }
    public string? ExternalStatus { get; set; }
    public DateTime? ExternalResponseAt { get; set; }

    public string? Notes { get; set; }

    protected TravelAllowancePayment() { }

    public TravelAllowancePayment(Guid id) : base(id)
    {
        Status = PaymentStatus.Draft;
    }
}
