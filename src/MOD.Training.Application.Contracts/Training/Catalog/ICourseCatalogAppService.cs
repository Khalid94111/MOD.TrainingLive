using MOD.Training.Training.Catalog.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Catalog;

public interface ICourseCatalogAppService : ICrudAppService<
    CourseCatalogDto,
    Guid,
    CourseCatalogGetListInput,
    CreateUpdateCourseCatalogDto>
{
    Task<List<CatalogEnrollmentConditionDto>> GetConditionsAsync(Guid catalogCourseId);
    Task<CatalogEnrollmentConditionDto> AddConditionAsync(Guid catalogCourseId, CreateUpdateCatalogEnrollmentConditionDto input);
    Task RemoveConditionAsync(Guid conditionId);
}
