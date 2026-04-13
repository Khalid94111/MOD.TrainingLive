using System;
using Volo.Abp.Domain.Entities;
using MOD.Training.Training.Enums;

namespace MOD.Training.Training.Plans;

public class SessionCondition : Entity<Guid>
{
    public Guid SessionId { get; set; }
    public ConditionType ConditionType { get; set; }
    public string ConditionValue { get; set; } = string.Empty;

    public CourseSession? Session { get; set; }

    protected SessionCondition() { }

    public SessionCondition(Guid id, Guid sessionId, ConditionType conditionType, string conditionValue)
        : base(id)
    {
        SessionId = sessionId;
        ConditionType = conditionType;
        ConditionValue = conditionValue;
    }
}
