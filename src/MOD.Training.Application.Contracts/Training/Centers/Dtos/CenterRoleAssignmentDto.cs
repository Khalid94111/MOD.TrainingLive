using MOD.Training.Training.Enums;
using System;
 using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Centers.Dtos;

public class CenterRoleAssignmentDto : EntityDto<Guid>
{
    public Guid CenterId { get; set; }
    public CenterRoleType RoleType { get; set; }
    public CenterAssignmentType AssignmentType { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? ServiceNumber { get; set; }
    public string? EmployeeNameAr { get; set; }
    public string? EmployeeNameEn { get; set; }
    public string? RankName { get; set; }
    public Guid? PositionId { get; set; }
    public string? PositionNameAr { get; set; }
    public string? PositionNameEn { get; set; }
}
