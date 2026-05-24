using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MOD.Training.Training.Consts;

namespace MOD.Training.Training.AnnualPlanSessions.Dtos;

// Phase 4C-α (v4.10.0) — External sessions are born Planned with NO dates; PAGE B-2
// posts this payload. Dates are set atomically later via CourseSession.SelectPriceQuoteAsync
// (step 5) when Staff picks the winning quote.
public class CreateExternalSessionDto
{
    [Required]
    public Guid TrainingPlanItemId { get; set; }

    public List<NomineeSubstitutionDto> Substitutions { get; set; } = new();

    [MaxLength(TrainingConsts.MaxNotesLength)]
    public string? Notes { get; set; }
}
