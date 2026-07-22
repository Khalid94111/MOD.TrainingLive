namespace MOD.Training.Training.Enums;

// Phase 4C-α (v4.10.0) — execution-progress badge for CourseSession list/detail.
// Distinct from Phase 4B-β's ExecutionStage (casual courses) because sessions never reach
// AwaitingReallocationApproval (sessions don't generate reallocations — §8 non-regression
// rule), and they need a dedicated NoExecutionPending state for Cancelled rows.
//
// Numbering deliberately starts at 1 to leave 0 unused — the DTO field is non-nullable
// and a 0 value signals "not computed", a useful sentinel during partial materialisation.
public enum SessionExecutionStage
{
    AwaitingQuoteSelection    = 1,   // External + Planned status
    AwaitingTravelCompletion  = 2,   // International + Travel request not completed
    AwaitingTravelAllowances  = 3,   // Travel completed + imported allowances incomplete
    AwaitingCoursePayment     = 4,   // CoursePayment null OR Draft
    AwaitingCompletion        = 5,   // InProgress, all payments Confirmed
    FinanciallyComplete       = 6,   // Completed/FinanciallyClosed with all payments done
    NoExecutionPending        = 7    // Cancelled
}
