using MOD.Training.Training.Catalog.Dtos;
using MOD.Training.Training.TenantCourses.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.TenantCourses;

public interface ITenantCourseAppService : IApplicationService
{
    Task<TenantCourseDto> GetAsync(Guid id);
    Task<PagedResultDto<TenantCourseDto>> GetListAsync(TenantCourseGetListInput input);
    Task<List<TenantCourseDto>> AddFromCatalogAsync(AddFromCatalogDto input);
    Task<TenantCourseDto> UpdateAsync(Guid id, UpdateTenantCourseDto input);
    Task DeleteAsync(Guid id);
    Task<PagedResultDto<CourseCatalogDto>> GetAvailableCatalogCoursesAsync(PagedAndSortedResultRequestDto input);
}
