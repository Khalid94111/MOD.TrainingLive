using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.TenantCourses.Dtos;

public class TenantCourseConditionDto : EntityDto<Guid>
{
    public Guid TenantCourseId { get; set; }
    public ConditionType ConditionType { get; set; }
    public string ConditionValue { get; set; } = null!;
    public bool IsActive { get; set; }
}
