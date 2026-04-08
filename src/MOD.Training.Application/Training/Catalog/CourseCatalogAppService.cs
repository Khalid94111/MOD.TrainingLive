using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Catalog.Dtos;
using MOD.Training.Training.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using YourApp.Training;

namespace MOD.Training.Training.Catalog;

[Authorize(TrainingPermissions.CourseCatalog.Default)]
public class CourseCatalogAppService(
    IRepository<CourseCatalog, Guid> catalogRepo,
    IRepository<CatalogEnrollmentCondition, Guid> conditionRepo)
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

        var count = await conditionRepo.CountAsync(c => c.CatalogCourseId == id);
        return MapToDto(entity, count);
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

        var catalogIds = items.Select(x => x.Id).ToList();
        var countMap = await GetConditionsCountMapAsync(catalogIds);

        return new PagedResultDto<CourseCatalogDto>(
            totalCount,
            items.Select(x => MapToDto(x, countMap.GetValueOrDefault(x.Id, 0))).ToList());
    }

    [Authorize(TrainingPermissions.CourseCatalog.Create)]
    public async Task<CourseCatalogDto> CreateAsync(CreateUpdateCourseCatalogDto input)
    {
        await ValidateUniqueNameAsync(input.CourseNameAr, input.CourseNameEn, null);
        var entity = input.ToEntity();
        entity = await catalogRepo.InsertAsync(entity, autoSave: true);
        return await GetAsync(entity.Id);
    }

    [Authorize(TrainingPermissions.CourseCatalog.Update)]
    public async Task<CourseCatalogDto> UpdateAsync(Guid id, CreateUpdateCourseCatalogDto input)
    {
        var entity = await catalogRepo.GetAsync(id);
        await ValidateUniqueNameAsync(input.CourseNameAr, input.CourseNameEn, id);
        input.MapTo(entity);
        await catalogRepo.UpdateAsync(entity, autoSave: true);
        return await GetAsync(id);
    }

    [Authorize(TrainingPermissions.CourseCatalog.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await catalogRepo.DeleteAsync(id);
    }

    // ═══════════════════════════════════════════
    // CONDITIONS
    // ═══════════════════════════════════════════

    public async Task<List<CatalogEnrollmentConditionDto>> GetConditionsAsync(Guid catalogCourseId)
    {
        var conditions = await conditionRepo.GetListAsync(c => c.CatalogCourseId == catalogCourseId);
        return conditions.Select(c => c.ToDto()).ToList();
    }

    [Authorize(TrainingPermissions.CourseCatalog.Update)]
    public async Task<CatalogEnrollmentConditionDto> AddConditionAsync(
        Guid catalogCourseId, CreateUpdateCatalogEnrollmentConditionDto input)
    {
        await catalogRepo.GetAsync(catalogCourseId);

        var condition = new CatalogEnrollmentCondition
        {
            CatalogCourseId = catalogCourseId,
            ConditionType = input.ConditionType,
            ConditionValue = input.ConditionValue,
        };

        condition = await conditionRepo.InsertAsync(condition, autoSave: true);
        return condition.ToDto();
    }

    [Authorize(TrainingPermissions.CourseCatalog.Update)]
    public async Task RemoveConditionAsync(Guid conditionId)
    {
        await conditionRepo.DeleteAsync(conditionId);
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

    private async Task<Dictionary<Guid, int>> GetConditionsCountMapAsync(List<Guid> catalogIds)
    {
        var queryable = await conditionRepo.GetQueryableAsync();
        var groups = await AsyncExecuter.ToListAsync(
            queryable.Where(c => catalogIds.Contains(c.CatalogCourseId))
                .GroupBy(c => c.CatalogCourseId)
                .Select(g => new { Id = g.Key, Count = g.Count() }));
        return groups.ToDictionary(x => x.Id, x => x.Count);
    }

    private static CourseCatalogDto MapToDto(CourseCatalog entity, int conditionsCount)
    {
        var dto = entity.ToDto();
        dto.FieldNameAr = entity.Field?.FieldNameAr;
        dto.FieldNameEn = entity.Field?.FieldNameEn;
        dto.ConditionsCount = conditionsCount;
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
