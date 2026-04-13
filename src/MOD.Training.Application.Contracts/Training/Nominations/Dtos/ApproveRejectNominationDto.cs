using MOD.Training.Training.Consts;
using System.ComponentModel.DataAnnotations;
 
namespace MOD.Training.Training.Nominations.Dtos;

public class ApproveRejectNominationDto
{
    [MaxLength(TrainingConsts.MaxNotesLength)]
    public string? Notes { get; set; }
}
