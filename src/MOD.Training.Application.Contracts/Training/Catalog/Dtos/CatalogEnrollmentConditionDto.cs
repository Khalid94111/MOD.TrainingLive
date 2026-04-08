using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Catalog.Dtos;

public class CatalogEnrollmentConditionDto : EntityDto<Guid>
{
    public Guid CatalogCourseId { get; set; }
    public ConditionType ConditionType { get; set; }
    public string ConditionValue { get; set; } = null!;
}

public class CreateUpdateCatalogEnrollmentConditionDto
{
    public ConditionType ConditionType { get; set; }
    public string ConditionValue { get; set; } = null!;
}
