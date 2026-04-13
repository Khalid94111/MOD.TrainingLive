using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Catalog;
using MOD.Training.Training.Catalog.Dtos;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.TenantCourses.Dtos;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace MOD.Training.Training.TenantCourses;

[Authorize(TrainingPermissions.TenantCourses.Default)]
public class TenantCourseAppService(
    IRepository<TenantCourse, Guid> tenantCourseRepo,
    IRepository<TenantCourseCondition, Guid> conditionRepo,
    IRepository<CourseCatalog, Guid> catalogRepo,
    IRepository<CatalogEnrollmentCondition, Guid> catalogConditionRepo)
    : ApplicationService, ITenantCourseAppService
{
    public async Task<TenantCourseDto> GetAsync(Guid id)
    {
        var queryable = await tenantCourseRepo.WithDetailsAsync(
            x => x.CatalogCourse!, x => x.CatalogCourse!.Field!);
        var entity = await AsyncExecuter.FirstOrDefaultAsync(queryable.Where(x => x.Id == id))
            ?? throw new BusinessException("Training:TenantCourse:NotFound");

        var count = await conditionRepo.CountAsync(c => c.TenantCourseId == id);
        return MapToDto(entity, count);
    }

    public async Task<PagedResultDto<TenantCourseDto>> GetListAsync(TenantCourseGetListInput input)
    {
        var queryable = await tenantCourseRepo.WithDetailsAsync(
            x => x.CatalogCourse!, x => x.CatalogCourse!.Field!);
        queryable = ApplyFilters(queryable, input);

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        var items = await AsyncExecuter.ToListAsync(
            queryable
                .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "CatalogCourse.CourseNameAr" : input.Sorting)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount));

        var tcIds = items.Select(x => x.Id).ToList();
        var countMap = await GetConditionsCountMapAsync(tcIds);

        return new PagedResultDto<TenantCourseDto>(
            totalCount,
            items.Select(x => MapToDto(x, countMap.GetValueOrDefault(x.Id, 0))).ToList());
    }

    /// <summary>
    /// Add courses from catalog. Auto-copies conditions (locked).
    /// POST /api/app/tenant-course
    /// </summary>
    [Authorize(TrainingPermissions.TenantCourses.Create)]
    public async Task<List<TenantCourseDto>> AddFromCatalogAsync(AddFromCatalogDto input)
    {
        var result = new List<TenantCourseDto>();

        foreach (var catalogCourseId in input.CatalogCourseIds)
        {
            if (await tenantCourseRepo.AnyAsync(x => x.CatalogCourseId == catalogCourseId))
                throw new BusinessException("Training:TenantCourse:AlreadyAdded")
                    .WithData("catalogCourseId", catalogCourseId);

            var catalogCourse = await catalogRepo.GetAsync(catalogCourseId);
            if (!catalogCourse.IsActive)
                throw new BusinessException("Training:TenantCourse:CatalogCourseInactive");

            var tenantCourse = new TenantCourse
            {
                CatalogCourseId = catalogCourseId,
                ResultType = catalogCourse.ResultType,
                RequiresEvaluation = catalogCourse.RequiresEvaluation,
                RequiresProviderEvaluation = catalogCourse.RequiresProviderEvaluation,
                HasCertificate = catalogCourse.HasCertificate,
                EvaluationBlocksCertificate = catalogCourse.EvaluationBlocksCertificate,
                IsActive = true,
                AddedById = CurrentUser.GetId(),
                AddedAt = Clock.Now,
            };

            tenantCourse = await tenantCourseRepo.InsertAsync(tenantCourse, autoSave: true);

            // Auto-copy catalog conditions → locked tenant conditions
            var catalogConditions = await catalogConditionRepo.GetListAsync(
                c => c.CatalogCourseId == catalogCourseId);

            foreach (var catCond in catalogConditions)
            {
                await conditionRepo.InsertAsync(new TenantCourseCondition
                {
                    TenantCourseId = tenantCourse.Id,
                    ConditionType = catCond.ConditionType,
                    ConditionValue = catCond.ConditionValue,
                    IsActive = true,
                }, autoSave: true);
            }

            result.Add(await GetAsync(tenantCourse.Id));
        }

        return result;
    }

    [Authorize(TrainingPermissions.TenantCourses.Update)]
    public async Task<TenantCourseDto> UpdateAsync(Guid id, UpdateTenantCourseDto input)
    {
        var entity = await tenantCourseRepo.GetAsync(id);
        input.MapTo(entity);
        await tenantCourseRepo.UpdateAsync(entity, autoSave: true);
        return await GetAsync(id);
    }

    [Authorize(TrainingPermissions.TenantCourses.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await tenantCourseRepo.DeleteAsync(id);
    }

    /// <summary>
    /// Read-only locked conditions.
    /// GET /api/app/tenant-course/{tenantCourseId}/conditions
    /// </summary>
    public async Task<List<TenantCourseConditionDto>> GetConditionsAsync(Guid tenantCourseId)
    {
        var conditions = await conditionRepo.GetListAsync(c => c.TenantCourseId == tenantCourseId);
        return conditions.Select(c => c.ToDto()).ToList();
    }

    /// <summary>
    /// Active catalog courses NOT yet added to this tenant.
    /// GET /api/app/tenant-course/available-catalog-courses
    /// </summary>
    public async Task<PagedResultDto<CourseCatalogDto>> GetAvailableCatalogCoursesAsync(
        PagedAndSortedResultRequestDto input)
    {
        var existingIds = (await tenantCourseRepo.GetQueryableAsync())
            .Select(x => x.CatalogCourseId);

        var queryable = await catalogRepo.WithDetailsAsync(x => x.Field!);
        queryable = queryable.Where(x => x.IsActive && !existingIds.Contains(x.Id));

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        var items = await AsyncExecuter.ToListAsync(
            queryable
                .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? nameof(CourseCatalog.CourseNameAr) : input.Sorting)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount));

        return new PagedResultDto<CourseCatalogDto>(
            totalCount,
            items.Select(x =>
            {
                var dto = x.ToDto();
                dto.FieldNameAr = x.Field?.FieldNameAr;
                dto.FieldNameEn = x.Field?.FieldNameEn;
                return dto;
            }).ToList());
    }

    // ═══════════════════════════════════════════
    // PRIVATE
    // ═══════════════════════════════════════════

    private async Task<Dictionary<Guid, int>> GetConditionsCountMapAsync(List<Guid> tcIds)
    {
        var queryable = await conditionRepo.GetQueryableAsync();
        var groups = await AsyncExecuter.ToListAsync(
            queryable.Where(c => tcIds.Contains(c.TenantCourseId))
                .GroupBy(c => c.TenantCourseId)
                .Select(g => new { Id = g.Key, Count = g.Count() }));
        return groups.ToDictionary(x => x.Id, x => x.Count);
    }

    private static TenantCourseDto MapToDto(TenantCourse entity, int conditionsCount)
    {
        var dto = entity.ToDto();
        dto.CatalogCourseNameAr = entity.CatalogCourse?.CourseNameAr ?? string.Empty;
        dto.CatalogCourseNameEn = entity.CatalogCourse?.CourseNameEn ?? string.Empty;
        dto.CatalogCourseFieldNameAr = entity.CatalogCourse?.Field?.FieldNameAr;
        dto.CatalogCourseCategory = entity.CatalogCourse?.Category;
        dto.ConditionsCount = conditionsCount;
        return dto;
    }

    private static IQueryable<TenantCourse> ApplyFilters(
        IQueryable<TenantCourse> query, TenantCourseGetListInput input)
    {
        if (!input.Filter.IsNullOrWhiteSpace())
        {
            var filter = input.Filter!.Trim();
            query = query.Where(x =>
                x.CatalogCourse!.CourseNameAr.Contains(filter) ||
                x.CatalogCourse!.CourseNameEn.Contains(filter));
        }
        if (input.FieldId.HasValue)
            query = query.Where(x => x.CatalogCourse!.FieldId == input.FieldId.Value);
        if (input.ResultType.HasValue)
            query = query.Where(x => x.ResultType == input.ResultType.Value);
        if (input.IsActive.HasValue)
            query = query.Where(x => x.IsActive == input.IsActive.Value);
        return query;
    }
}
