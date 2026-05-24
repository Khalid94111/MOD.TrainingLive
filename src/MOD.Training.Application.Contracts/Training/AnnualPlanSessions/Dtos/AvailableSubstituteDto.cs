using System;

namespace MOD.Training.Training.AnnualPlanSessions.Dtos;

// Phase 4C-α (v4.10.0) — one row in the substitution dialog dropdown. Filtered to
// same-rank-as-original active employees in the same tenant, excluding employees already
// nominated on the parent plan item (no double-nomination).
public class AvailableSubstituteDto
{
    public Guid EmployeeId { get; set; }
    public Guid RankId { get; set; }
    public string ServiceNumber { get; set; } = string.Empty;
    public string FullNameAr { get; set; } = string.Empty;
    public string FullNameEn { get; set; } = string.Empty;
    public string? RankNameAr { get; set; }
}
