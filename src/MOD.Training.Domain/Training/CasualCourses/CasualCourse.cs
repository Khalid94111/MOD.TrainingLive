using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.CasualCourses;

public class CasualCourse : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid TenantCourseId { get; set; }
    public Guid UnitId { get; set; }
    public Guid RequestedById { get; set; }

    public CourseType CourseType { get; set; }
    public int Priority { get; set; }
    public string Justification { get; set; } = "";
    public string? DescriptionAr { get; set; }
    public string? ObjectivesAr { get; set; }

    public int DurationYears { get; set; }
    public int DurationMonths { get; set; }
    public int DurationDays { get; set; }

    public DateTime EstimatedDateFrom { get; set; }
    public DateTime EstimatedDateTo { get; set; }

    public string? FundingSourceName { get; set; }
    public string? FundingSourceVoteCode { get; set; }

    public decimal? CourseCost { get; set; }

    public decimal? EstimatedTotalCost { get; set; }
    public FundingScenario? FundingScenario { get; set; }

    public Guid? SelectedPriceQuoteId { get; set; }

    // Phase 4B-α — actual confirmed dates; written by Staff at quote-winner selection.
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }

    // Approval and execution are separate lifecycles. Approval remains THApproved while
    // execution advances Planned -> Scheduled -> InProgress -> Completed.
    public SessionStatus ExecutionStatus { get; set; } = SessionStatus.Planned;

    public CasualCourseStatus Status { get; set; } = CasualCourseStatus.Draft;
    public CasualCourseStatus? ReturnedFromStatus { get; set; }

    public bool IsReturned { get; set; }
    public Guid? LastReturnNoteId { get; set; }

    public string? RejectedReason { get; set; }

    public ICollection<CasualCourseFinancialItem>? FinancialItems { get; set; }
    public ICollection<CasualCourseNomination>? Nominations { get; set; }

    protected CasualCourse() { }

    public CasualCourse(
        Guid id,
        Guid tenantCourseId,
        Guid unitId,
        Guid requestedById,
        CourseType courseType,
        int priority,
        string justification,
        int durationDays,
        DateTime from,
        DateTime to) : base(id)
    {
        TenantCourseId = tenantCourseId;
        UnitId = unitId;
        RequestedById = requestedById;
        CourseType = courseType;
        Priority = priority;
        Justification = justification;
        DurationDays = durationDays;
        EstimatedDateFrom = from;
        EstimatedDateTo = to;
    }
}
