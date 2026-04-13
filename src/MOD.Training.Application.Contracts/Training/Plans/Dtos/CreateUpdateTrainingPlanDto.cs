using System;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.Plans.Dtos;

public class CreateUpdateTrainingPlanDto
{
    [Required]
    public int Year { get; set; }

    public DateTime? OpenDate { get; set; }

    public DateTime? CloseDate { get; set; }
}
