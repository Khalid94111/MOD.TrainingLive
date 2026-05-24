using MOD.Training.Training.Plans.Dtos;
using System.Collections.Generic;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class CasualCourseDetailDto : CasualCourseDto
{
    public List<CasualCourseNominationDto> Nominations { get; set; } = new();
    public List<CasualCourseFinancialItemDto> FinancialItems { get; set; } = new();
    public PlanNoteDto? LatestReturnNote { get; set; }
    public string? ConditionSummary { get; set; }

    // Phase 4C-α Patch 1 (v4.10.1) — Course Info Bar fields.
    // Officer/Enlisted split derived from each nominee's Rank.PersonnelType.
    // FundingScenarioLabel is the localised (Arabic) short label for the picked scenario.
    public int OfficersCount { get; set; }
    public int EnlistedCount { get; set; }
    public string? FundingScenarioLabel { get; set; }
}
