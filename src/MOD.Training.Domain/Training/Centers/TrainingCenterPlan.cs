using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Centers
{
    public class TrainingCenterPlan : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public Guid CenterId { get; set; }
        public int Year { get; set; }
        public CenterPlanStatus Status { get; set; } = CenterPlanStatus.Draft;
        public DateTime? OpenedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public Guid? OpenedById { get; set; }
        public Guid? SubmittedById { get; set; }
        public Guid? ApprovedById { get; set; }
    }
}
