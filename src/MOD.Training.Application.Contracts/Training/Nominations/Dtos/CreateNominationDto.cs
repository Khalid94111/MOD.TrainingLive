using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.Nominations.Dtos;

public class CreateNominationDto
{
    [Required]
    public Guid SessionId { get; set; }

    [Required]
    [MinLength(1)]
    public List<Guid> EmployeeIds { get; set; } = [];
}
