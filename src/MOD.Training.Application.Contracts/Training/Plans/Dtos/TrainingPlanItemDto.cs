using System;
using Volo.Abp.Application.Dtos;
using MOD.Training.Training.Enums;

namespace MOD.Training.Training.Plans.Dtos;

public class TrainingPlanItemDto : FullAuditedEntityDto<Guid>
{
    public Guid PlanId { get; set; }
    public Guid TenantCourseId { get; set; }
    public string TenantCourseNameAr { get; set; } = string.Empty;
    public string TenantCourseNameEn { get; set; } = string.Empty;
    public CourseType CourseType { get; set; }
    public PreferredQuarter PreferredQuarter { get; set; }
    public int Priority { get; set; }
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
    public decimal EstimatedCost { get; set; }
    public string? FundingSource { get; set; }
    public Guid SubmittedById { get; set; }
    public string SubmittedByName { get; set; } = string.Empty;
    public string? SubmittedByRank { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public Guid? TrainingCenterPlanItemId { get; set; }
    public string? SourceCenterName { get; set; }
    public string? SourceTenantName { get; set; }
    public int FinancialItemCount { get; set; }

    // CHG-01: derived from nominations count
    public int NomineesCount { get; set; }

    // CHG-05
    public bool IsReturned { get; set; }
}
