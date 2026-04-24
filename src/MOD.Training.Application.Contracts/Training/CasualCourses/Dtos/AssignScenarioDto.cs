using MOD.Training.Training.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.CasualCourses.Dtos;

/// <summary>
/// Staff review payload for PAGE 4.3 (v4.6.0).
/// UTM already entered the full breakdown at creation, so Staff's job narrows to
/// picking a scenario + optionally adjusting a handful of rank rows.
/// </summary>
public class AssignScenarioDto
{
    [Required]
    public FundingScenario FundingScenario { get; set; }

    /// <summary>
    /// Targeted rank-row edits only — no whole-tree upsert.
    /// </summary>
    public List<StaffAdjustmentDto>? Adjustments { get; set; }

    /// <summary>
    /// Commit=false keeps the course in UnderReview (draft of review).
    /// Commit=true transitions UnderReview → StaffReviewed and enforces EstimatedTotalCost > 0.
    /// </summary>
    public bool Commit { get; set; }
}
