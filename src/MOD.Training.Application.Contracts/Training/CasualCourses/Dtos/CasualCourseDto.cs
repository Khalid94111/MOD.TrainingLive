using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class CasualCourseDto : FullAuditedEntityDto<Guid>
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

    // Phase 4B-α — actual confirmed dates (set by SelectPriceQuoteAsync after provider confirms).
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }

    public CasualCourseStatus Status { get; set; }
    public CasualCourseStatus? ReturnedFromStatus { get; set; }

    public bool IsReturned { get; set; }
    public Guid? LastReturnNoteId { get; set; }

    public string? RejectedReason { get; set; }

    // Resolved display fields
    public string CourseNameAr { get; set; } = "";
    public string UnitName { get; set; } = "";
    public string RequesterName { get; set; } = "";
    public int NomineesCount { get; set; }
    public int FinancialItemsCount { get; set; }
    public string? LatestReturnReason { get; set; }

    // Phase 4B-β Patch 2 — Execution stage (computed server-side; null for pre-THApproved rows).
    // Frontend resolves the localized label from the enum; for the two progress-bearing stages
    // (AwaitingTravelAllowances, AwaitingReallocationApproval) the server also surfaces
    // "current/total" counters so the UI can render "2/5 confirmed" without a second query.
    public ExecutionStage? ExecutionStage { get; set; }
    public int? ExecutionStageProgressCurrent { get; set; }
    public int? ExecutionStageProgressTotal { get; set; }
}
