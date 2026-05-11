using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Payments;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Managers;

/// <summary>
/// Heart of Phase 4B-β. Auto-generates BudgetReallocation rows when a CoursePayment for a
/// casual course confirms. Called only from <c>CoursePaymentAppService.ConfirmAsync</c>.
///
/// Properties:
/// • Idempotent — re-running for the same coursePaymentId returns 0 without writing.
/// • Scenario-aware — uses the existing FundingScenario enum + FinancialAmountSource column.
///     Scenario 1 (FundingSourceCoversAll)  → no rows generated.
///     Scenario 2 (FundingSourceCoversCourse) → skip items where Source == FundingSource
///                                              (CourseCost stays on the funding source).
///     Scenario 3 (FinancialItemsCoverAll)   → one row per item.
/// • Audit-friendly — copies FundingSourceVoteCode from the course at generation time
///     (not joined), so historical reallocations are stable if the funding source changes later.
/// • Sums per item — one reallocation per FinancialItem, summed across all rank rows.
///
/// Annual-plan sessions never reach here — CoursePaymentAppService.ConfirmAsync only invokes
/// this when CoursePayment.CasualCourseId.HasValue is true.
/// </summary>
public class BudgetReallocationGenerator(
    IRepository<CasualCourse, Guid> courseRepo,
    IRepository<CasualCourseFinancialItem, Guid> itemRepo,
    IRepository<BudgetReallocation, Guid> reallocRepo,
    IGuidGenerator guidGenerator,
    ICurrentTenant currentTenant)
    : DomainService
{
    /// <summary>
    /// Returns the number of BudgetReallocation rows written. Returns 0 for scenario 1,
    /// for an already-processed payment (idempotency), or when every item sums to 0.
    /// Throws BusinessException if the course's FundingScenario is unset (scenario 2/3 only)
    /// or its FundingSourceVoteCode is missing.
    /// </summary>
    public async Task<int> GenerateForCasualCoursePaymentAsync(
        Guid coursePaymentId,
        Guid casualCourseId)
    {
        // Idempotency guard — defense against double-Confirm clicks.
        var alreadyGenerated = await reallocRepo.AnyAsync(r => r.CoursePaymentId == coursePaymentId);
        if (alreadyGenerated) return 0;

        var course = await courseRepo.GetAsync(casualCourseId);

        // Scenario 1 — funding source covers everything → no reallocation needed.
        if (course.FundingScenario == FundingScenario.FundingSourceCoversAll)
            return 0;

        if (course.FundingScenario == null)
            throw new BusinessException("Training:Reallocation:ScenarioNotSet");

        if (string.IsNullOrEmpty(course.FundingSourceVoteCode))
            throw new BusinessException("Training:Reallocation:VoteCodeRequired");

        // Load items with rank breakdowns — Ranks navigation is paired in EF config.
        var query = (await itemRepo.WithDetailsAsync(i => i.Ranks!))
            .Where(i => i.CasualCourseId == casualCourseId);
        var items = await AsyncExecuter.ToListAsync(query);

        var generated = 0;
        foreach (var item in items)
        {
            // Scenario 2 — skip items already paid from the funding source.
            // Scenario 3 — process all items.
            if (course.FundingScenario == FundingScenario.FundingSourceCoversCourse
                && item.Source == FinancialAmountSource.FundingSource)
            {
                continue;
            }

            var amount = item.Ranks?.Sum(r => r.SubtotalOMR) ?? 0m;
            if (amount <= 0) continue;

            var realloc = new BudgetReallocation(guidGenerator.Create())
            {
                TenantId = currentTenant.Id,
                CasualCourseId = casualCourseId,
                CoursePaymentId = coursePaymentId,
                FundingSourceVoteCode = course.FundingSourceVoteCode!,
                ToFinancialItemId = item.FinancialItemId,
                AmountOMR = amount,
                Status = ReallocationStatus.Pending
            };
            await reallocRepo.InsertAsync(realloc);
            generated++;
        }

        return generated;
    }
}
