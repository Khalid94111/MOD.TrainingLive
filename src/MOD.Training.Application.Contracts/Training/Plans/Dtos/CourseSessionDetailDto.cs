using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Plans.Dtos;

// Phase 4C-α (v4.10.0) — full detail payload for PAGE D. Adds nominations and the
// optional selected-quote summary on top of the list-row shape.
public class CourseSessionDetailDto : FullAuditedEntityDto<Guid>
{
    public Guid? TrainingPlanItemId { get; set; }
    public Guid? TrainingCenterPlanItemId { get; set; }

    public Guid TenantCourseId { get; set; }
    public string? TenantCourseNameAr { get; set; }
    public string? TenantCourseNameEn { get; set; }

    public CourseType CourseType { get; set; }

    public int PreferredQuarter { get; set; }
    public int PlanYear { get; set; }

    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public DateTime? EstimatedDateFrom { get; set; }
    public DateTime? EstimatedDateTo { get; set; }

    public SessionStatus Status { get; set; }
    public SessionExecutionStage ExecutionStage { get; set; }

    public Guid? SelectedPriceQuoteId { get; set; }
    public decimal? SelectedPriceQuoteAmountOMR { get; set; }
    public Guid? SelectedPriceQuoteProviderId { get; set; }
    public string? SelectedPriceQuoteProviderNameAr { get; set; }

    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledById { get; set; }

    public List<SessionNominationDto> Nominations { get; set; } = new();

    // Phase 4C-α Patch 1 (v4.10.1) — Course Info Bar fields.
    // DurationDays sourced from the originating TrainingPlanItem (sessions have no
    // direct duration column). ApprovedCostOMR is computed via PlanItemCostCalculator.
    // For sessions, UnitTotalOMR == ApprovedCostOMR (one session per plan item).
    // UnitName resolved from TrainingPlanItem.UnitId (the requesting unit on the plan).
    public int DurationDays { get; set; }
    public int NomineesCount { get; set; }
    public int OfficersCount { get; set; }
    public int EnlistedCount { get; set; }
    public decimal ApprovedCostOMR { get; set; }
    public decimal UnitTotalOMR { get; set; }
    public string? UnitName { get; set; }
}
