using System;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.Centers.Dtos;

public class CreateUpdateCenterPlanWindowDto
{
    [Required]
    public int Year { get; set; }

    [Required]
    public DateTime OpenDate { get; set; }

    [Required]
    public DateTime CloseDate { get; set; }
}
