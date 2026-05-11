namespace MOD.Training.Training.Enums;

/// <summary>
/// Lifecycle of a BudgetReallocation row. Auto-generated as Pending; Staff transitions to
/// Approved once TD has signed the paperwork externally. Approved is terminal.
/// </summary>
public enum ReallocationStatus
{
    Pending  = 0,
    Approved = 1
}
