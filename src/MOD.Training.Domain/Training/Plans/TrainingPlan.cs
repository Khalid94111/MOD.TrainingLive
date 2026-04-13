using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
 
namespace MOD.Training.Training.Plans;

public class TrainingPlan : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public int Year { get; set; }
    public PlanStatus Status { get; set; }
    public DateTime? OpenDate { get; set; }
    public DateTime? CloseDate { get; set; }

    protected TrainingPlan() { }

    public TrainingPlan(Guid id, int year) : base(id)
    {
        Year = year;
        Status = PlanStatus.Draft;
    }
}
