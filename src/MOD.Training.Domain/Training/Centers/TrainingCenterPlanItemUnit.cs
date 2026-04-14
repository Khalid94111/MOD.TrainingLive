using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Domain.Entities;

namespace MOD.Training.Training.Centers
{
    public class TrainingCenterPlanItemUnit : Entity<Guid>
    {
        public TrainingCenterPlanItemUnit(Guid id) : base(id)
        {
            
        }
        public TrainingCenterPlanItemUnit()
        {
            
        }
        public Guid PlanItemId { get; set; }
        public Guid UnitId { get; set; }
    }
}
