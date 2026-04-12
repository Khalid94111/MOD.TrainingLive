using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Centers
{
    public class CenterPlanWindow : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public int Year { get; set; }
        public DateTime OpenDate { get; set; }
        public DateTime CloseDate { get; set; }
        public Guid OpenedById { get; set; }
        public DateTime OpenedAt { get; set; }

        public bool IsOpen => DateTime.Now >= OpenDate && DateTime.Now <= CloseDate;
    }
}
