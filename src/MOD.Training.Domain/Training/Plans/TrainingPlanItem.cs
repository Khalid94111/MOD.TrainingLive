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
    public string? FundingSource { get; set; }
    public Guid SubmittedById { get; set; }
    public Guid? UnitId { get; set; }

    // CHG-05
    public bool IsReturned { get; set; }
    public Guid? LastReturnNoteId { get; set; }

    public TrainingPlan? Plan { get; set; }

    protected TrainingPlanItem() { }

    public TrainingPlanItem(
        Guid id,
        Guid planId,
        Guid tenantCourseId,
        CourseType courseType,
        PreferredQuarter preferredQuarter,
        int priority,
        string justification,
        Guid submittedById) : base(id)
    {
        PlanId = planId;
        TenantCourseId = tenantCourseId;
        CourseType = courseType;
        PreferredQuarter = preferredQuarter;
        Priority = priority;
        Justification = justification;
        SubmittedById = submittedById;
    }
}
