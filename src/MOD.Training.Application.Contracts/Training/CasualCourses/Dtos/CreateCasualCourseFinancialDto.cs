using System;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class CreateCasualCourseFinancialDto
{
    [Required]
    public Guid FinancialItemId { get; set; }

    [Range(0.0, (double)decimal.MaxValue)]
    public decimal EstimatedAmountOMR { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
