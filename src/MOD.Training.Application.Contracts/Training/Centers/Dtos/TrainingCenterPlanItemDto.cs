using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Centers.Dtos;

public class TrainingCenterPlanItemDto : FullAuditedEntityDto<Guid>
{
    public Guid PlanId { get; set; }
    public Guid TenantCourseId { get; set; }
    public string TenantCourseName { get; set; } = null!;
    public DateTime EstimatedStartDate { get; set; }
    public DateTime EstimatedEndDate { get; set; }
    public int Capacity { get; set; }
    public int DurationWeeks { get; set; }
    public string? Objective { get; set; }
    public BeneficiaryType BeneficiaryType { get; set; }
    public int BatchNumber { get; set; }
    public List<Guid> UnitIds { get; set; } = [];
}
