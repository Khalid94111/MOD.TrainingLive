using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Centers.Dtos;

public class TrainingCenterPlanDto : FullAuditedEntityDto<Guid>
{
    public Guid CenterId { get; set; }
    public string CenterName { get; set; } = null!;
    public int Year { get; set; }
    public CenterPlanStatus Status { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? OpenedById { get; set; }
    public Guid? SubmittedById { get; set; }
    public Guid? ApprovedById { get; set; }
    public string? RejectionReason { get; set; }
    public string? ReturnReason { get; set; }
    public int ItemCount { get; set; }
    public List<TrainingCenterPlanItemDto> Items { get; set; } = [];
}

public class PlanActionReasonDto
{
    public string Reason { get; set; } = string.Empty;
}
