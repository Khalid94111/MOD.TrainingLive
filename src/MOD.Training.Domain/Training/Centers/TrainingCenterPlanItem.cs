using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Domain.Entities.Auditing;

namespace MOD.Training.Training.Centers
{
    public class TrainingCenterPlanItem : FullAuditedEntity<Guid>
    {
        public Guid PlanId { get; set; }
        public Guid TenantCourseId { get; set; }
        public DateTime EstimatedStartDate { get; set; }
        public DateTime EstimatedEndDate { get; set; }
        public int Capacity { get; set; }
        public int DurationWeeks { get; set; }
        public string? Objective { get; set; }
        public BeneficiaryType BeneficiaryType { get; set; }
        public int BatchNumber { get; set; }
    }

}
