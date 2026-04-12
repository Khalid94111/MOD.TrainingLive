using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Centers.Dtos;

public class TrainingCenterDto : FullAuditedEntityDto<Guid>
{
    public Guid OrgUnitId { get; set; }
    public string OrgUnitName { get; set; } = null!;
    public string CenterNameAr { get; set; } = null!;
    public string CenterNameEn { get; set; } = null!;
    public string? Location { get; set; }
    public bool IsActive { get; set; }
    public List<CenterRoleAssignmentDto> RoleAssignments { get; set; } = [];
    public int TcoCount { get; set; }
    public string? TcmName { get; set; }
}
