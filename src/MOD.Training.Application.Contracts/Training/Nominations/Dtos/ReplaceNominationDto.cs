using System;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.Nominations.Dtos;

public class ReplaceNominationDto
{
    [Required]
    public Guid NewEmployeeId { get; set; }
}
