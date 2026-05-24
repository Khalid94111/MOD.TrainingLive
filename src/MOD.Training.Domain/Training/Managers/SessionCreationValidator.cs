using System;
using System.Linq;
using System.Threading.Tasks;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Hr;
using MOD.Training.Training.Plans;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace MOD.Training.Training.Managers;

// Phase 4C-α (v4.10.0) — domain service that gates session creation from a TrainingPlanItem.
//
// Two checks live here:
//   ValidateForCreationAsync — guards CreateInternal/External: parent plan must be THApproved
//     and no active session may already exist for the item (Q3: strict 1:1).
//   ValidateSubstitutionAsync — guards each nominee substitution at session creation:
//     replacement must share the original's rank (Q-D, Q-E: same-rank only, at creation only).
//
// DatesMatchPreferredQuarter is a soft check exposed for the create-internal flow's date
// validation; it returns a bool rather than throwing because the prompt's Q-F + non-regression
// rules treat quarter mismatch as a warning, not a hard block.
public class SessionCreationValidator(
    IRepository<TrainingPlanItem, Guid> planItemRepo,
    IRepository<TrainingPlan, Guid> planRepo,
    IRepository<CourseSession, Guid> sessionRepo,
    IRepository<Employee, Guid> employeeRepo)
    : DomainService
{
    public async Task ValidateForCreationAsync(Guid trainingPlanItemId)
    {
        var item = await planItemRepo.GetAsync(trainingPlanItemId);

        // Plan-level gate: only THApproved plans can spawn sessions.
        var plan = await planRepo.GetAsync(item.PlanId);
        if (plan.Status != PlanStatus.THApproved)
            throw new BusinessException("Training:Session:PlanNotApproved");

        // Uniqueness gate (Q3): exactly one active session per plan item.
        // Cancelled sessions don't count — Staff can retry creation after a cancel.
        var alreadyExists = await sessionRepo.AnyAsync(s =>
            s.TrainingPlanItemId == trainingPlanItemId &&
            s.Status != SessionStatus.Cancelled);

        if (alreadyExists)
            throw new BusinessException("Training:Session:AlreadyExists");
    }

    public async Task ValidateSubstitutionAsync(Guid originalEmployeeId, Guid replacementEmployeeId)
    {
        // No-op: same employee is "no substitution" and trivially valid.
        if (originalEmployeeId == replacementEmployeeId) return;

        var original = await employeeRepo.GetAsync(originalEmployeeId);
        var replacement = await employeeRepo.GetAsync(replacementEmployeeId);

        if (original.RankId != replacement.RankId)
            throw new BusinessException("Training:Session:RankMismatch");
    }

    // Soft check — returns false to drive a UI warning, never throws. Per §6 non-regression
    // rule: "Do not enforce date-quarter match strictly. Soft warning OK; don't block."
    public bool DatesMatchPreferredQuarter(DateTime startDate, int preferredQuarter, int planYear)
    {
        if (preferredQuarter is < 1 or > 4) return false;
        var quarterStart = new DateTime(planYear, (preferredQuarter - 1) * 3 + 1, 1);
        var quarterEnd = quarterStart.AddMonths(3).AddDays(-1);
        return startDate >= quarterStart && startDate <= quarterEnd;
    }
}
