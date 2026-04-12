using System;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.Centers.Dtos;

public class CreateUpdateCenterPlanDto
{
    [Required]
    public Guid CenterId { get; set; }

    [Required]
    public int Year { get; set; }
}
