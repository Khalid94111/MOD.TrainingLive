using System;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class AssignmentRankLineDto
{
    public Guid? Id { get; set; }

    /// <summary>
    /// Guid.Empty for flat (non-per-nominee) items.
    /// </summary>
    public Guid RankId { get; set; }

    [Range(0, int.MaxValue)]
    public int NomineeCount { get; set; }

    [Range(0.0, (double)decimal.MaxValue)]
    public decimal RatePerUnitOMR { get; set; }
}
