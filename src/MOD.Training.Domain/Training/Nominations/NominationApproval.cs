using System;
using Volo.Abp.Domain.Entities.Auditing;
using MOD.Training.Training.Enums;

namespace MOD.Training.Training.Nominations;

public class NominationApproval : CreationAuditedEntity<Guid>
{
    public Guid NominationId { get; set; }
    public int ApprovalLevel { get; set; } // 1=UTM, 2=UGM, 3=TD
    public Guid? ApprovedById { get; set; }
    public ApprovalStatus Status { get; set; }
    public DateTime? ActionDate { get; set; }
    public string? Notes { get; set; }

    public Nomination? Nomination { get; set; }

    protected NominationApproval() { }

    public NominationApproval(
        Guid id,
        Guid nominationId,
        int approvalLevel) : base(id)
    {
        NominationId = nominationId;
        ApprovalLevel = approvalLevel;
        Status = ApprovalStatus.Pending;
    }
}
