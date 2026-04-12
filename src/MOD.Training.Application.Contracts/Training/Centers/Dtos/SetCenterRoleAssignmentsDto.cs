using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.Centers.Dtos;

public class SetCenterRoleAssignmentsDto
{
    [Required]
    public List<CenterRoleAssignmentInputDto> Assignments { get; set; } = [];
}

public class CenterRoleAssignmentInputDto
{
    [Required]
    public CenterRoleType RoleType { get; set; }

    [Required]
    public CenterAssignmentType AssignmentType { get; set; }

    public Guid? EmployeeId { get; set; }
    public string? ServiceNumber { get; set; }
    public Guid? PositionId { get; set; }
}
