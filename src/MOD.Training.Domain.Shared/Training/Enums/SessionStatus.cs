namespace MOD.Training.Training.Enums;

// Phase 4C-α (v4.10.0): renumbered from Phase 3's 4-value enum.
// New value Planned=0 covers external sessions whose dates are pending quote selection.
// FinanciallyClosed=5 covers terminal post-payment state mirroring CasualCourseStatus.
public enum SessionStatus
{
    Planned           = 0,
    Scheduled         = 1,
    InProgress        = 2,
    Completed         = 3,
    Cancelled         = 4,
    FinanciallyClosed = 5
}
