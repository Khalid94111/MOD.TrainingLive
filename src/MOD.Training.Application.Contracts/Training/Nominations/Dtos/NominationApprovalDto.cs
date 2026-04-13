using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;
 
namespace MOD.Training.Training.Nominations.Dtos;

public class NominationApprovalDto : CreationAuditedEntityDto<Guid>
{
    public Guid NominationId { get; set; }
    public int ApprovalLevel { get; set; }
    public string ApprovalLevelName { get; set; } = string.Empty; // UTM/UGM/TD
    public Guid? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public ApprovalStatus Status { get; set; }
    public DateTime? ActionDate { get; set; }
    public string? Notes { get; set; }
}
