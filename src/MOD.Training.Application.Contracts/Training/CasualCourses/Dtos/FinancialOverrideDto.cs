using System;

namespace MOD.Training.Training.CasualCourses.Dtos;

/// <summary>
/// UTM's per-rank rate override applied during CreateAsync/UpdateAsync.
/// Targets a single rank row under a single financial item.
/// </summary>
public class FinancialOverrideDto
{
    public Guid FinancialItemId { get; set; }

    /// <summary>
    /// Guid.Empty for flat (non-per-nominee) items — matches the synthetic rank row.
    /// </summary>
    public Guid RankId { get; set; }

    public decimal RatePerUnitOMR { get; set; }

    public string? Notes { get; set; }
}
