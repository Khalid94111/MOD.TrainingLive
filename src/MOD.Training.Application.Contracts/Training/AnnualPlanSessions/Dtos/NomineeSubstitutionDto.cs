using System;

namespace MOD.Training.Training.AnnualPlanSessions.Dtos;

// Phase 4C-α (v4.10.0) — one substitution row in the create-session flow.
// Validated server-side via SessionCreationValidator.ValidateSubstitutionAsync (same-rank only).
public class NomineeSubstitutionDto
{
    public Guid OriginalEmployeeId { get; set; }
    public Guid ReplacementEmployeeId { get; set; }
    public string? Reason { get; set; }
}
