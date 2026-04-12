using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.Centers.Dtos;

public class SetPlanItemUnitsDto
{
    [Required]
    public List<Guid> UnitIds { get; set; } = [];
}
