using System.ComponentModel.DataAnnotations;
using MOD.Training.Training.Consts;

namespace MOD.Training.Training.Plans.Dtos;

// Phase 4C-α (v4.10.0) — reason is required so the cancellation audit trail is meaningful.
public class CancelSessionDto
{
    [Required]
    [MaxLength(TrainingConsts.MaxNotesLength)]
    public string Reason { get; set; } = string.Empty;
}
