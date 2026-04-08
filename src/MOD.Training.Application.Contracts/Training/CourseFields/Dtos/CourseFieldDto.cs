using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.CourseFields.Dtos;

public class CourseFieldDto : FullAuditedEntityDto<Guid>
{
    public string FieldNameAr { get; set; } = null!;
    public string FieldNameEn { get; set; } = null!;
    public bool IsActive { get; set; }
}

public class CreateUpdateCourseFieldDto
{
    public string FieldNameAr { get; set; } = null!;
    public string FieldNameEn { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
