using System;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.Plans.Dtos;

public class UpdateEstimatedCostDto
{
    [Required]
    [Range(0, double.MaxValue)]
    public decimal EstimatedCost { get; set; }
}
