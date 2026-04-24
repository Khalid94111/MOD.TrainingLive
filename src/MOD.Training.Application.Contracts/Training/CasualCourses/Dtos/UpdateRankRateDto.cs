using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class UpdateRankRateDto
{
    [Range(0.0, (double)decimal.MaxValue)]
    public decimal RatePerUnitOMR { get; set; }
}
