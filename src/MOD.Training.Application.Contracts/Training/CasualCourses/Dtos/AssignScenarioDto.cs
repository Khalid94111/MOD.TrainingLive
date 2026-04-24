using MOD.Training.Training.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class AssignScenarioDto
{
    [Required]
    public FundingScenario FundingScenario { get; set; }

    public List<AssignmentLineDto> FinancialItems { get; set; } = new();

    public bool Commit { get; set; }
}
