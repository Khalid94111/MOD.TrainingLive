using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Plans;

// Phase 4C-α (v4.10.0): session-level nominee snapshot, independent from Phase 3's
// Nomination workflow (which lives at the plan-item level). Created when a session
// is spawned: nominees are copied from the source plan item, optionally substituted
// (same-rank only — Q-D/Q-E), and locked. After session creation, this list is immutable.
//
// RankId is snapshotted from the employee at creation so substitution validation has a
// stable reference even if the employee's rank later changes in HR.
public class SessionNomination : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid SessionId { get; set; }

    public Guid EmployeeId { get; set; }
    public Guid OriginalEmployeeId { get; set; }
    public Guid RankId { get; set; }

    public string? SubstitutionReason { get; set; }

    public bool WasSubstituted => EmployeeId != OriginalEmployeeId;

    protected SessionNomination() { }

    public SessionNomination(Guid id) : base(id) { }
}
