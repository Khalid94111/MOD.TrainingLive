using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance.Dtos;
using MOD.Training.Training.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Finance;

[Authorize(TrainingPermissions.CourseTypeFinancialDefaults.Default)]
public class CourseTypeFinancialDefaultAppService(
    IRepository<CourseTypeFinancialItemDefault, Guid> defaultRepo,
    IRepository<FinancialItem, Guid> financialItemRepo)
    : ApplicationService, ICourseTypeFinancialDefaultAppService
{
    public async Task<List<CourseTypeFinancialItemDefaultDto>> GetListAsync(CourseType courseType)
    {
        var queryable = await defaultRepo.WithDetailsAsync(x => x.FinancialItem);
        var items = await AsyncExecuter.ToListAsync(
            queryable.Where(x => x.CourseType == courseType)
                     .OrderBy(x => x.SortOrder));

        return items.Select(x => new CourseTypeFinancialItemDefaultDto
        {
            Id = x.Id,
            CourseType = x.CourseType,
            FinancialItemId = x.FinancialItemId,
            FinancialItemNameAr = x.FinancialItem.NameAr,
            FinancialItemNameEn = x.FinancialItem.NameEn,
            SortOrder = x.SortOrder
        }).ToList();
    }

    [Authorize(TrainingPermissions.CourseTypeFinancialDefaults.Create)]
    public async Task<CourseTypeFinancialItemDefaultDto> CreateAsync(CreateCourseTypeFinancialItemDefaultDto input)
    {
        // Validate: only ExternalInternational or ExternalLocal
        if (input.CourseType == CourseType.Internal)
        {
            throw new BusinessException("Training:Defaults:InternalNotAllowed");
        }

        // Validate: financial item must be a sub-item (has ParentId)
        var financialItem = await financialItemRepo.GetAsync(input.FinancialItemId);
        if (!financialItem.ParentId.HasValue)
        {
            throw new BusinessException("Training:Defaults:OnlySubItems");
        }

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
}
