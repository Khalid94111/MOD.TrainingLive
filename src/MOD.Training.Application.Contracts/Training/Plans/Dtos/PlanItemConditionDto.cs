using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;
 
namespace MOD.Training.Training.Plans.Dtos;

public class PlanItemConditionDto : EntityDto<Guid>
{
    public Guid PlanItemId { get; set; }
    public ConditionType ConditionType { get; set; }
    public string ConditionValue { get; set; } = string.Empty;
}
