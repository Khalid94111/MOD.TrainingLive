using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Finance.Dtos;

public class CourseTypeFinancialItemDefaultDto : EntityDto<Guid>
{
    public CourseType CourseType { get; set; }
    public Guid FinancialItemId { get; set; }
    public string FinancialItemNameAr { get; set; } = default!;
    public string FinancialItemNameEn { get; set; } = default!;
    public int SortOrder { get; set; }
}

public class CreateCourseTypeFinancialItemDefaultDto
{
    public CourseType CourseType { get; set; }
    public Guid FinancialItemId { get; set; }
}
