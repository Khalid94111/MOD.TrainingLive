using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MOD.Training.Training.Consts;

namespace MOD.Training.Training.AnnualPlanSessions.Dtos;

// Phase 4C-α (v4.10.0) — Internal sessions are born Scheduled with confirmed dates.
// PAGE B-1 posts this payload; CreateInternalSessionAsync enforces CourseType==Internal.
public class CreateInternalSessionDto
{
    [Required]
    public Guid TrainingPlanItemId { get; set; }

    [Required]
    public DateTime ActualStartDate { get; set; }

    [Required]
    public DateTime ActualEndDate { get; set; }

    public List<NomineeSubstitutionDto> Substitutions { get; set; } = new();

    [MaxLength(TrainingConsts.MaxNotesLength)]
    public string? Notes { get; set; }
}
