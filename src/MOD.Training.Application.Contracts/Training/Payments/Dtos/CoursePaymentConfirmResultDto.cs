namespace MOD.Training.Training.Payments.Dtos;

/// <summary>
/// Returned by <c>CoursePaymentAppService.ConfirmAsync</c>. Carries the updated payment plus
/// the count of <c>BudgetReallocation</c> rows produced by the auto-generator (0 for sessions
/// and for casual courses under Scenario 1).
/// </summary>
public class CoursePaymentConfirmResultDto
{
    public CoursePaymentDto Payment { get; set; } = default!;
    public int GeneratedReallocationsCount { get; set; }
}
