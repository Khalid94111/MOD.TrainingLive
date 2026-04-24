using System;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class CreateCasualCourseFinancialDto
{
    [Required]
    public Guid FinancialItemId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
