namespace MOD.Training.Training.Enums;

/// <summary>
/// Lifecycle of a TravelAllowancePayment or a CoursePayment row.
/// Phase 4B-β — values are immutable on the wire, do not renumber.
/// </summary>
public enum PaymentStatus
{
    Draft     = 0,   // Finance entered amounts, not yet confirmed; row is mutable.
    Confirmed = 1,   // Finance signed off — terminal for audit; CoursePayment.Confirm also fires reallocation generator.
    Cancelled = 2    // Reserved; no transition exposed in 4B-β endpoints.
}
