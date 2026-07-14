using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Catalog.Dtos;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.TenantCourses;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Saas.Tenants;

namespace MOD.Training.Training.Catalog;

[Authorize(TrainingPermissions.CourseCatalog.Default)]
public class CourseCatalogAppService(
    IRepository<CourseCatalog, Guid> catalogRepo,
    IRepository<TenantCourse, Guid> tenantCourseRepo,
    IRepository<Tenant, Guid> tenantRepo,
    IMapper mapper)
    : ApplicationService, ICourseCatalogAppService
{
    // ═══════════════════════════════════════════
    // CRUD
    // ═══════════════════════════════════════════

    public async Task<CourseCatalogDto> GetAsync(Guid id)
    {
        var queryable = await catalogRepo.WithDetailsAsync(x => x.Field!);
        var entity = await AsyncExecuter.FirstOrDefaultAsync(queryable.Where(x => x.Id == id))
            ?? throw new BusinessException("Training:CourseCatalog:NotFound");

        return MapToDto(entity);
    }

    public async Task<PagedResultDto<CourseCatalogDto>> GetListAsync(CourseCatalogGetListInput input)
    {
        var queryable = await catalogRepo.WithDetailsAsync(x => x.Field!);
        queryable = ApplyFilters(queryable, input);

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        var items = await AsyncExecuter.ToListAsync(
            queryable
                .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? nameof(CourseCatalog.CourseNameAr) : input.Sorting)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount));

        return new PagedResultDto<CourseCatalogDto>(
            totalCount,
            items.Select(MapToDto).ToList());
    }

    [Authorize(TrainingPermissions.CourseCatalog.Create)]
    public async Task<CourseCatalogDto> CreateAsync(CreateUpdateCourseCatalogDto input)
    {
        await ValidateUniqueNameAsync(input.CourseNameAr, input.CourseNameEn, null);
        var entity = mapper.Map<CourseCatalog>(input);
        entity = await catalogRepo.InsertAsync(entity, autoSave: true);
        return await GetAsync(entity.Id);
    }

    [Authorize(TrainingPermissions.CourseCatalog.Update)]
    public async Task<CourseCatalogDto> UpdateAsync(Guid id, CreateUpdateCourseCatalogDto input)
    {
        var entity = await catalogRepo.GetAsync(id);
        await ValidateUniqueNameAsync(input.CourseNameAr, input.CourseNameEn, id);
        mapper.Map(input, entity);
        await catalogRepo.UpdateAsync(entity, autoSave: true);
        return await GetAsync(id);
    }

    [Authorize(TrainingPermissions.CourseCatalog.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await catalogRepo.DeleteAsync(id);
    }

    // ═══════════════════════════════════════════
    // SUBSCRIBED TENANTS
    // ═══════════════════════════════════════════

    [Authorize(TrainingPermissions.CourseCatalog.ViewSubscribedTenants)]
    public async Task<List<CourseCatalogSubscribedTenantDto>> GetSubscribedTenantsAsync(Guid catalogCourseId)
    {
        await catalogRepo.GetAsync(catalogCourseId);

        using (CurrentTenant.Change(null))
        {
            var tcQueryable = await tenantCourseRepo.GetQueryableAsync();
            var tenantIds = await AsyncExecuter.ToListAsync(
                tcQueryable
                    .Where(x => x.CatalogCourseId == catalogCourseId && x.TenantId.HasValue)
                    .Select(x => x.TenantId!.Value)
                    .Distinct());

            if (tenantIds.Count == 0)
                return [];

            var tenantQueryable = await tenantRepo.GetQueryableAsync();
            var tenants = await AsyncExecuter.ToListAsync(
                tenantQueryable.Where(t => tenantIds.Contains(t.Id)));

            return tenants
                .Select(t => new CourseCatalogSubscribedTenantDto
                {
                    TenantId = t.Id,
                    TenantName = t.Name,
                })
                .OrderBy(t => t.TenantName)
                .ToList();
        }
    }

    // ═══════════════════════════════════════════
    // PRIVATE
    // ═══════════════════════════════════════════

    private async Task ValidateUniqueNameAsync(string nameAr, string nameEn, Guid? excludeId)
    {
        if (excludeId.HasValue)
        {
            if (await catalogRepo.AnyAsync(x => x.CourseNameAr == nameAr && x.Id != excludeId.Value))
                throw new BusinessException("Training:CourseCatalog:DuplicateNameAr").WithData("name", nameAr);
            if (await catalogRepo.AnyAsync(x => x.CourseNameEn == nameEn && x.Id != excludeId.Value))
                throw new BusinessException("Training:CourseCatalog:DuplicateNameEn").WithData("name", nameEn);
        }
        else
        {
            if (await catalogRepo.AnyAsync(x => x.CourseNameAr == nameAr))
                throw new BusinessException("Training:CourseCatalog:DuplicateNameAr").WithData("name", nameAr);
            if (await catalogRepo.AnyAsync(x => x.CourseNameEn == nameEn))
                throw new BusinessException("Training:CourseCatalog:DuplicateNameEn").WithData("name", nameEn);
        }
    }

    private CourseCatalogDto MapToDto(CourseCatalog entity)
    {
        var dto = mapper.Map<CourseCatalogDto>(entity);
        dto.FieldNameAr = entity.Field?.FieldNameAr;
        dto.FieldNameEn = entity.Field?.FieldNameEn;
        return dto;
    }

    private static IQueryable<CourseCatalog> ApplyFilters(
        IQueryable<CourseCatalog> query, CourseCatalogGetListInput input)
    {
        if (!input.Filter.IsNullOrWhiteSpace())
        {
            var filter = input.Filter!.Trim();
            query = query.Where(x => x.CourseNameAr.Contains(filter) || x.CourseNameEn.Contains(filter));
        }
        if (input.FieldId.HasValue)
            query = query.Where(x => x.FieldId == input.FieldId.Value);
        if (!input.Category.IsNullOrWhiteSpace())
            query = query.Where(x => x.Category == input.Category);
        if (input.IsActive.HasValue)
            query = query.Where(x => x.IsActive == input.IsActive.Value);
        return query;
    }
}
