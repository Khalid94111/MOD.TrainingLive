using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Centers
{
    public class CenterRoleAssignment : FullAuditedEntity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public Guid CenterId { get; set; }
        public CenterRoleType RoleType { get; set; }
        public CenterAssignmentType AssignmentType { get; set; }
        public Guid? EmployeeId { get; set; }
        public string? ServiceNumber { get; set; }
        public Guid? PositionId { get; set; }
    }
}
