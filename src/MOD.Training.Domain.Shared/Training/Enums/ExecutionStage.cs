namespace MOD.Training.Training.Enums;

/// <summary>
/// Where a post-THApproved casual course sits in its execution lifecycle.
/// Computed (not stored) — see <c>CasualCourseAppService.ComputeExecutionStage</c>.
/// External-flow courses traverse stages 1-5 then land on <see cref="FinanciallyComplete"/>;
/// internal-flow courses skip stages 1-3 (no quote, no travel, no reallocations).
/// </summary>
public enum ExecutionStage
{
    AwaitingQuoteSelection       = 1,
    AwaitingTravelInstruction    = 2,
    AwaitingTravelAllowances     = 3,
    AwaitingCoursePayment        = 4,
    AwaitingReallocationApproval = 5,
    FinanciallyComplete          = 6
}
