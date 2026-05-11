using System;

namespace MOD.Training.Training.Payments.Dtos;

/// <summary>
/// Payload for creating or updating a travel-allowance payment in Draft state.
/// Server controls TotalOMR (computed), PersonnelType (resolved), Status (lifecycle),
/// ConfirmedAt/ConfirmedById, and the External* Nebras fields — none of which appear here.
/// </summary>
public class CreateUpdateTravelAllowancePaymentDto
{
    // Polymorphic parent — exactly one must be set.
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }

    public Guid NominationId { get; set; }

    public decimal TicketAmountOMR { get; set; }
    public decimal TravelAllowanceOMR { get; set; }
    public decimal ClothingAllowanceOMR { get; set; }
    public decimal InsuranceOMR { get; set; }
    public decimal VisaFeesOMR { get; set; }

    public string? Notes { get; set; }
}
