using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Plans.Dtos;

public class TrainingPlanItemDto : FullAuditedEntityDto<Guid>
{
    public Guid PlanId { get; set; }
    public Guid TenantCourseId { get; set; }
    public string TenantCourseName { get; set; } = string.Empty; // Resolved in GetListAsync
    public CourseType CourseType { get; set; }
    public PreferredQuarter PreferredQuarter { get; set; }
    public int Priority { get; set; }
    public int OfficersCount { get; set; }
    public int EnlistedCount { get; set; }
    public int Capacity { get; set; }
    public string Justification { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string? ObjectivesAr { get; set; }
    public string? ObjectivesEn { get; set; }
    public int DurationYears { get; set; }
    public int DurationMonths { get; set; }
    public int DurationDays { get; set; }
    public DateTime? EstimatedDateFrom { get; set; }
    public DateTime? EstimatedDateTo { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? FundingSource { get; set; }
    public Guid SubmittedById { get; set; }
    public string SubmittedByName { get; set; } = string.Empty; // Resolved
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; } // Resolved
}
