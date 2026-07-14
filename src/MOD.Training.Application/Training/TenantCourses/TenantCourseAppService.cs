using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using AutoMapper;
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
    IRepository<CourseCatalog, Guid> catalogRepo,
    IMapper mapper)
    : ApplicationService, ITenantCourseAppService
{
    public async Task<TenantCourseDto> GetAsync(Guid id)
    {
        var queryable = await tenantCourseRepo.WithDetailsAsync(
            x => x.CatalogCourse!, x => x.CatalogCourse!.Field!);
        var entity = await AsyncExecuter.FirstOrDefaultAsync(queryable.Where(x => x.Id == id))
            ?? throw new BusinessException("Training:TenantCourse:NotFound");

        return MapToDto(entity);
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

        return new PagedResultDto<TenantCourseDto>(
            totalCount,
            items.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Add courses from catalog.
    /// POST /api/app/tenant-course
    /// </summary>
    [Authorize(TrainingPermissions.TenantCourses.Create)]
    public async Task<List<TenantCourseDto>> AddFromCatalogAsync(AddFromCatalogDto input)
    {
        var result = new List<TenantCourseDto>();
        var currentTenantId = CurrentTenant.Id;

        foreach (var catalogCourseId in input.CatalogCourseIds)
        {
            // Explicit tenant scoping — mirrors the unique index (TenantId, CatalogCourseId)
            // instead of relying solely on ABP's implicit multi-tenancy filter.
            if (await tenantCourseRepo.AnyAsync(x =>
                    x.TenantId == currentTenantId && x.CatalogCourseId == catalogCourseId))
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
            result.Add(await GetAsync(tenantCourse.Id));
        }

        return result;
    }

    [Authorize(TrainingPermissions.TenantCourses.Update)]
    public async Task<TenantCourseDto> UpdateAsync(Guid id, UpdateTenantCourseDto input)
    {
        var entity = await tenantCourseRepo.GetAsync(id);
        mapper.Map(input, entity);
        await tenantCourseRepo.UpdateAsync(entity, autoSave: true);
        return await GetAsync(id);
    }

    [Authorize(TrainingPermissions.TenantCourses.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await tenantCourseRepo.DeleteAsync(id);
    }

    /// <summary>
    /// Active catalog courses NOT yet added to this tenant.
    /// GET /api/app/tenant-course/available-catalog-courses
    /// </summary>
    public async Task<PagedResultDto<CourseCatalogDto>> GetAvailableCatalogCoursesAsync(
        PagedAndSortedResultRequestDto input)
    {
        var currentTenantId = CurrentTenant.Id;
        var existingIds = (await tenantCourseRepo.GetQueryableAsync())
            .Where(x => x.TenantId == currentTenantId)
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
                var dto = mapper.Map<CourseCatalogDto>(x);
                dto.FieldNameAr = x.Field?.FieldNameAr;
                dto.FieldNameEn = x.Field?.FieldNameEn;
                return dto;
            }).ToList());
    }

    // ═══════════════════════════════════════════
    // PRIVATE
    // ═══════════════════════════════════════════

    private TenantCourseDto MapToDto(TenantCourse entity)
    {
        var dto = mapper.Map<TenantCourseDto>(entity);
        dto.CatalogCourseNameAr = entity.CatalogCourse?.CourseNameAr ?? string.Empty;
        dto.CatalogCourseNameEn = entity.CatalogCourse?.CourseNameEn ?? string.Empty;
        dto.CatalogCourseFieldNameAr = entity.CatalogCourse?.Field?.FieldNameAr;
        dto.CatalogCourseCategory = entity.CatalogCourse?.Category;
        dto.AddedAtFormatted = entity.AddedAt.ToString("yyyy-MM-dd HH:mm", global::System.Globalization.CultureInfo.InvariantCulture);
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
