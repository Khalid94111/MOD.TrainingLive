using MOD.Training.Training.CourseFields.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.CourseFields;

public interface ICourseFieldAppService : ICrudAppService<
    CourseFieldDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateCourseFieldDto>
{
    /// <summary>
    /// Returns all active fields — used for dropdown lookups.
    /// </summary>
    Task<List<CourseFieldDto>> GetAllActiveAsync();
}
