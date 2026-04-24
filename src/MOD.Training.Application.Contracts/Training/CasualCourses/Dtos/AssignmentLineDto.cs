using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class AssignmentLineDto
{
    public Guid? Id { get; set; }

    [Required]
    public Guid FinancialItemId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public List<AssignmentRankLineDto> Ranks { get; set; } = new();
}
