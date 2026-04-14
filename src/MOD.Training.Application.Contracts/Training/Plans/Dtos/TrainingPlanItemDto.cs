using System;
using Volo.Abp.Application.Dtos;
using MOD.Training.Training.Enums;

namespace MOD.Training.Training.Plans.Dtos;

public class TrainingPlanItemDto : FullAuditedEntityDto<Guid>
{
    public Guid PlanId { get; set; }
    public Guid TenantCourseId { get; set; }
    public string TenantCourseNameAr { get; set; } = string.Empty; // Resolved via CourseNameResolver
    public string TenantCourseNameEn { get; set; } = string.Empty; // Resolved via CourseNameResolver
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
    public decimal EstimatedCost { get; set; } // Computed: SUM(PlanItemFinancialItem.EstimatedAmountOMR)
    public string? FundingSource { get; set; }
    public Guid SubmittedById { get; set; }
    public string SubmittedByName { get; set; } = string.Empty; // Resolved via EmployeeResolver
    public string? SubmittedByRank { get; set; }                 // Resolved via EmployeeResolver
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; } // Resolved via OrgUnit
    public int FinancialItemCount { get; set; } // Count of assigned financial items
}
