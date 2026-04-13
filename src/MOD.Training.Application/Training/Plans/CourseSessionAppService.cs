using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans.Dtos;
using MOD.Training.Training.TenantCourses;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
 

namespace MOD.Training.Training.Plans;

[Authorize(TrainingPermissions.CourseSession.Default)]
public class CourseSessionAppService(
    IRepository<CourseSession, Guid> repository,
    IRepository<Course, Guid> courseRepository,
    IRepository<TenantCourse, Guid> tenantCourseRepository,
    CourseSessionToDtoMapper toDtoMapper,
    CreateUpdateCourseSessionToEntityMapper toEntityMapper)
    : ApplicationService, ICourseSessionAppService
{
    public async Task<CourseSessionDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        var dto = toDtoMapper.Map(entity);
        await EnrichDtoAsync(dto, entity);
        return dto;
    }

    public async Task<PagedResultDto<CourseSessionDto>> GetListAsync(CourseSessionGetListInput input)
    {
        var queryable = await repository.GetQueryableAsync();

        if (input.CourseId.HasValue)
            queryable = queryable.Where(x => x.CourseId == input.CourseId.Value);
        if (input.Status.HasValue)
            queryable = queryable.Where(x => x.Status == input.Status.Value);

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        queryable = queryable.OrderByDescending(x => x.StartDate);
        queryable = queryable.PageBy(input);

        var entities = await AsyncExecuter.ToListAsync(queryable);

        // Batch load course names
        var courseIds = entities.Select(x => x.CourseId).Distinct().ToList();
        var courseQueryable = await courseRepository.GetQueryableAsync();
        var courses = await AsyncExecuter.ToListAsync(
            courseQueryable.Where(x => courseIds.Contains(x.Id)));

        var tcIds = courses.Select(x => x.TenantCourseId).Distinct().ToList();
        var tcQueryable = await tenantCourseRepository.GetQueryableAsync();
        var tenantCourses = await AsyncExecuter.ToListAsync(
            tcQueryable.Where(x => tcIds.Contains(x.Id)));

        var courseTcMap = courses.ToDictionary(x => x.Id, x => x.TenantCourseId);
        var tcLookup = tenantCourses.ToDictionary(x => x.Id);

        var dtos = entities.Select(e =>
        {
            var dto = toDtoMapper.Map(e);
            if (courseTcMap.TryGetValue(e.CourseId, out var tcId) &&
                tcLookup.TryGetValue(tcId, out var tc))
            {
                dto.CourseName = tc.Id.ToString(); // TODO: resolve catalog name
            }
            return dto;
        }).ToList();

        return new PagedResultDto<CourseSessionDto>(totalCount, dtos);
    }

    [Authorize(TrainingPermissions.CourseSession.Create)]
    public async Task<CourseSessionDto> CreateAsync(CreateUpdateCourseSessionDto input)
    {
        var entity = new CourseSession(
            GuidGenerator.Create(),
            input.CourseId,
            input.SessionCode,
            input.StartDate,
            input.EndDate,
            input.MaxSeats);
        entity.Location = input.Location;
        entity.Country = input.Country;
        entity.Cost = input.Cost;

        await repository.InsertAsync(entity, autoSave: true);
        return toDtoMapper.Map(entity);
    }

    [Authorize(TrainingPermissions.CourseSession.Update)]
    public async Task<CourseSessionDto> UpdateAsync(Guid id, CreateUpdateCourseSessionDto input)
    {
        var entity = await repository.GetAsync(id);
        toEntityMapper.Map(input, entity);
        entity.AvailableSeats = input.MaxSeats; // Reset available when max changes

        await repository.UpdateAsync(entity, autoSave: true);
        return toDtoMapper.Map(entity);
    }

    [Authorize(TrainingPermissions.CourseSession.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await repository.DeleteAsync(id);
    }

    private async Task EnrichDtoAsync(CourseSessionDto dto, CourseSession entity)
    {
        var course = await courseRepository.FindAsync(entity.CourseId);
        if (course != null)
        {
            var tc = await tenantCourseRepository.FindAsync(course.TenantCourseId);
            if (tc != null)
                dto.CourseName = tc.Id.ToString(); // TODO: resolve catalog name
        }
    }
}
