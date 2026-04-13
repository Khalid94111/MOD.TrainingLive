using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Catalog;
using MOD.Training.Training.CourseFields.Dtos;
using MOD.Training.Training.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
 
namespace MOD.Training.Training.CourseFields;

[Authorize(TrainingPermissions.CourseFields.Default)]
public class CourseFieldAppService(
    IRepository<CourseField, Guid> fieldRepo)
    : CrudAppService<
        CourseField,
        CourseFieldDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateCourseFieldDto>(fieldRepo),
    ICourseFieldAppService
{
    /// <summary>
    /// Returns all active fields for dropdown lookups.
    /// GET /api/app/course-field/all
    /// </summary>
    public async Task<List<CourseFieldDto>> GetAllActiveAsync()
    {
        var items = await Repository.GetListAsync(f => f.IsActive);
        return items.OrderBy(f => f.FieldNameAr).Select(f => f.ToDto()).ToList();
    }

    protected override async Task<IQueryable<CourseField>> CreateFilteredQueryAsync(
        PagedAndSortedResultRequestDto input)
    {
        return await Repository.GetQueryableAsync();
    }

    protected override Task<CourseField> MapToEntityAsync(CreateUpdateCourseFieldDto input)
    {
        return Task.FromResult(input.ToEntity());
    }

    protected override Task MapToEntityAsync(CreateUpdateCourseFieldDto input, CourseField entity)
    {
        input.MapTo(entity);
        return Task.CompletedTask;
    }

    protected override CourseFieldDto MapToGetOutputDto(CourseField entity)
    {
        return entity.ToDto();
    }

    protected override CourseFieldDto MapToGetListOutputDto(CourseField entity)
    {
        return entity.ToDto();
    }
}
