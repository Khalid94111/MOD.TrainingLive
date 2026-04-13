using System;
using Volo.Abp.Domain.Entities;
using MOD.Training.Training.Enums;

namespace MOD.Training.Training.Plans;

public class PlanItemCondition : Entity<Guid>
{
    public Guid PlanItemId { get; set; }
    public ConditionType ConditionType { get; set; }
    public string ConditionValue { get; set; } = string.Empty;

    public TrainingPlanItem? PlanItem { get; set; }

    protected PlanItemCondition() { }

    public PlanItemCondition(Guid id, Guid planItemId, ConditionType conditionType, string conditionValue)
        : base(id)
    {
        PlanItemId = planItemId;
        ConditionType = conditionType;
        ConditionValue = conditionValue;
    }
}
