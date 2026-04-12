using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Centers
{
    public class TrainingCenter : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public Guid OrgUnitId { get; set; }
        public string CenterNameAr { get; set; } = null!;
        public string CenterNameEn { get; set; } = null!;
        public string? Location { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
