using Microsoft.AspNetCore.Authorization;
using MOD.Training.Shared;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance.Dtos;
using MOD.Training.Training.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Finance;

[Authorize(TrainingPermissions.CourseTypeFinancialDefaults.Default)]
public class CourseTypeFinancialDefaultAppService(
    IRepository<CourseTypeFinancialItemDefault, Guid> defaultRepo,
    IRepository<FinancialItem, Guid> financialItemRepo)
    : ApplicationService, ICourseTypeFinancialDefaultAppService
{
    /// <summary>
    /// Lists defaults for a course type, enriched with financial item names.
    /// </summary>
    [Authorize(TrainingPermissions.CourseTypeFinancialDefaults.Default)]
    public async Task<ListResultDto<CourseTypeFinancialItemDefaultDto>> GetListAsync(
        CourseType courseType)
    {
        var queryable = await defaultRepo.GetQueryableAsync();
        queryable = queryable.Where(x => x.CourseType == courseType);

        var entities = await AsyncExecuter.ToListAsync(
            queryable.OrderBy(x => x.SortOrder)
        );

        if (entities.Count == 0)
        {
            return new ListResultDto<CourseTypeFinancialItemDefaultDto>([]);
        }

        var fiIds = entities.Select(e => e.FinancialItemId).Distinct().ToList();
        var financialItems = await financialItemRepo.GetListAsync(
            x => fiIds.Contains(x.Id)
        );
        var fiMap = financialItems.ToDictionary(f => f.Id);

        var dtos = entities.Select(entity =>
        {
            var dto = new CourseTypeFinancialItemDefaultDto
            {
                Id = entity.Id,
                CourseType = (CourseType)(int)entity.CourseType,
                FinancialItemId = entity.FinancialItemId,
                SortOrder = entity.SortOrder
            };

            if (fiMap.TryGetValue(entity.FinancialItemId, out var fi))
            {
                dto.FinancialItemNameAr = fi.NameAr;
                dto.FinancialItemNameEn = fi.NameEn;
                dto.FinancialItemCode = fi.Code;
            }

            return dto;
        }).ToList();

        return new ListResultDto<CourseTypeFinancialItemDefaultDto>(dtos);
    }

    [Authorize(TrainingPermissions.CourseTypeFinancialDefaults.Create)]
    public async Task<CourseTypeFinancialItemDefaultDto> CreateAsync(CreateCourseTypeFinancialItemDefaultDto input)
    {
        // Validate: only ExternalInternational or ExternalLocal
        if (input.CourseType == CourseType.Internal)
        {
            throw new BusinessException("Training:Defaults:InternalNotAllowed");
        }

        var financialItem = await financialItemRepo.GetAsync(input.FinancialItemId);

        // Validate: unique (CourseType + FinancialItemId) per tenant
        var exists = await defaultRepo.AnyAsync(x =>
            x.CourseType == input.CourseType &&
            x.FinancialItemId == input.FinancialItemId);

        if (exists)
        {
            throw new BusinessException("Training:Defaults:AlreadyAssigned");
        }

        // Get next sort order
        var maxSortOrder = 0;
        var queryable = await defaultRepo.GetQueryableAsync();
        var existingItems = await AsyncExecuter.ToListAsync(
            queryable.Where(x => x.CourseType == input.CourseType));

        if (existingItems.Count > 0)
        {
            maxSortOrder = existingItems.Max(x => x.SortOrder);
        }

        var entity = new CourseTypeFinancialItemDefault
        {
            CourseType = input.CourseType,
            FinancialItemId = input.FinancialItemId,
            SortOrder = maxSortOrder + 1
        };

        await defaultRepo.InsertAsync(entity);

        return new CourseTypeFinancialItemDefaultDto
        {
            Id = entity.Id,
            CourseType = entity.CourseType,
            FinancialItemId = entity.FinancialItemId,
            FinancialItemNameAr = financialItem.NameAr,
            FinancialItemNameEn = financialItem.NameEn,
            SortOrder = entity.SortOrder
        };
    }

    [Authorize(TrainingPermissions.CourseTypeFinancialDefaults.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await defaultRepo.DeleteAsync(id);
    }

    /// <summary>
    /// Batch-updates SortOrder after drag-and-drop reordering.
    /// </summary>
    [Authorize(TrainingPermissions.CourseTypeFinancialDefaults.Create)]
    public async Task UpdateSortOrderAsync(UpdateSortOrderInput input)
    {
        var ids = input.Items.Select(x => x.Id).ToList();
        var entities = await defaultRepo.GetListAsync(x => ids.Contains(x.Id));
        var entityMap = entities.ToDictionary(x => x.Id);

        foreach (var item in input.Items)
        {
            if (entityMap.TryGetValue(item.Id, out var entity))
            {
                entity.SortOrder = item.SortOrder;
            }
        }

        await defaultRepo.UpdateManyAsync(entities, autoSave: true);
    }
}
