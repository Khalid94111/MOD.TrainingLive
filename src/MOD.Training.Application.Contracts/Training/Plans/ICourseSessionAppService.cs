using MOD.Training.Training.Plans.Dtos;
using System;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Plans;

public interface ICourseSessionAppService : ICrudAppService<
    CourseSessionDto,
    Guid,
    CourseSessionGetListInput,
    CreateUpdateCourseSessionDto>
{
}
