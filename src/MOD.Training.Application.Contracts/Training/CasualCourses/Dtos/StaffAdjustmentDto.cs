using System;

namespace MOD.Training.Training.CasualCourses.Dtos;

/// <summary>
/// Targeted Staff adjustment to a single rank row during review (v4.6.0).
/// Replaces the full-tree AssignmentLineDto payload from v4.5.x.
/// </summary>
public class StaffAdjustmentDto
{
    public Guid CasualCourseFinancialItemRankId { get; set; }

    public decimal NewRatePerUnitOMR { get; set; }

    public string? AdjustmentNote { get; set; }
}
