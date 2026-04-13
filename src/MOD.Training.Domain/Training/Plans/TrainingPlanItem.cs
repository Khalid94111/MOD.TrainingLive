using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Plans;

public class TrainingPlanItem : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid PlanId { get; set; }
    public Guid TenantCourseId { get; set; }
    public CourseType CourseType { get; set; }
    public PreferredQuarter PreferredQuarter { get; set; }
    public int Priority { get; set; }
    public int OfficersCount { get; set; }
    public int EnlistedCount { get; set; }
    public int Capacity { get; set; } // Auto = OfficersCount + EnlistedCount
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
    public decimal? EstimatedCost { get; set; } // Entered by Staff during review
    public string? FundingSource { get; set; } // Casual only, free text
    public Guid SubmittedById { get; set; } // UTM user (MOD-17)
    public Guid? UnitId { get; set; } // Auto from logged-in user's OrgUnit

    public TrainingPlan? Plan { get; set; }

    protected TrainingPlanItem() { }

    public TrainingPlanItem(
        Guid id,
        Guid planId,
        Guid tenantCourseId,
        CourseType courseType,
        PreferredQuarter preferredQuarter,
        int priority,
        int officersCount,
        int enlistedCount,
        string justification,
        Guid submittedById) : base(id)
    {
        PlanId = planId;
        TenantCourseId = tenantCourseId;
        CourseType = courseType;
        PreferredQuarter = preferredQuarter;
        Priority = priority;
        OfficersCount = officersCount;
        EnlistedCount = enlistedCount;
        Capacity = officersCount + enlistedCount;
        Justification = justification;
        SubmittedById = submittedById;
    }
}
