using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Plans;

// Phase 4C-α (v4.10.0): session execution layer. A session is spawned from EITHER an
// approved annual TrainingPlanItem (Phase 4C-α) OR a TrainingCenterPlanItem (Phase 4C-β
// future) — the polymorphic source is enforced by a DB CHECK constraint.
//
// Internal sessions are born Scheduled with confirmed dates. External sessions are born
// Planned with null dates; SelectPriceQuoteAsync atomically sets dates + transitions to
// Scheduled. Cancellation is allowed only before InProgress (Q-C).
public class CourseSession : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    // Polymorphic source — exactly one of these is set (CHECK constraint enforced).
    public Guid? TrainingPlanItemId { get; set; }
    public Guid? TrainingCenterPlanItemId { get; set; }

    // Snapshotted from the source plan item at creation. Stable for the session's life.
    public Guid TenantCourseId { get; set; }
    public CourseType CourseType { get; set; }

    // Estimated/preferred window from the source plan item — used by Internal date validation
    // (soft check) and by overdue detection in the queue page.
    public int PreferredQuarter { get; set; }
    public int PlanYear { get; set; }

    // Confirmed dates. Nullable for external sessions in Planned status; populated atomically
    // alongside SelectedPriceQuoteId during SelectPriceQuoteAsync.
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }

    // Winning quote (external sessions only — null for Internal which has no quote stage).
    public Guid? SelectedPriceQuoteId { get; set; }

    public SessionStatus Status { get; set; }

    // Cancellation audit trio — written by CancelAsync only.
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledById { get; set; }

    public virtual ICollection<SessionNomination> Nominations { get; set; } = new List<SessionNomination>();

    protected CourseSession() { }

    public CourseSession(Guid id) : base(id) { }
}
