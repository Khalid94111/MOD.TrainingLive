using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class UpdateAmountDto
{
    [Range(0.0, (double)decimal.MaxValue)]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
