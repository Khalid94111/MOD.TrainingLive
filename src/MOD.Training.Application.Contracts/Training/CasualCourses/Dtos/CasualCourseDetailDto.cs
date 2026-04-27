using MOD.Training.Training.Plans.Dtos;
using System.Collections.Generic;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class CasualCourseDetailDto : CasualCourseDto
{
    public List<CasualCourseNominationDto> Nominations { get; set; } = new();
    public List<CasualCourseFinancialItemDto> FinancialItems { get; set; } = new();
    public PlanNoteDto? LatestReturnNote { get; set; }
    public string? ConditionSummary { get; set; }
}
