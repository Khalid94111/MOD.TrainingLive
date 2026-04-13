using MOD.Training.Training.Consts;
using System;
using System.ComponentModel.DataAnnotations;
 
namespace MOD.Training.Training.Plans.Dtos;

public class CreateUpdatePlanItemFinancialItemDto
{
    [Required]
    public Guid PlanItemId { get; set; }

    [Required]
    public Guid FinancialItemId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal EstimatedAmountOMR { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? EstimatedAmountUSD { get; set; }

    [MaxLength(TrainingConsts.MaxNotesLength)]
    public string? Notes { get; set; }
}
