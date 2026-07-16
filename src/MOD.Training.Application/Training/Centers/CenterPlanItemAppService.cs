using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Centers.Dtos;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.TenantCourses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Centers;

[Authorize(TrainingPermissions.CenterPlanItems.Default)]
public class CenterPlanItemAppService(
    IRepository<TrainingCenterPlanItem, Guid> repository,
    IRepository<TrainingCenterPlanItemUnit, Guid> unitRepository,
        IRepository<TenantCourse, Guid> tenantCourseRepository, 

    IRepository<TrainingCenterPlan, Guid> planRepository)
    : CrudAppService<
        TrainingCenterPlanItem,
        TrainingCenterPlanItemDto,
        Guid,
        CenterPlanItemGetListInput,
        CreateUpdateCenterPlanItemDto>(repository)
{
    private readonly TrainingCenterPlanItemToDtoMapper _toDtoMapper = new();
    private readonly CreateUpdateCenterPlanItemToEntityMapper _toEntityMapper = new();

    protected override async Task<IQueryable<TrainingCenterPlanItem>> CreateFilteredQueryAsync(
      CenterPlanItemGetListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        query = query.Where(x => x.PlanId == input.PlanId);
        return query;
    }

    public override async Task<PagedResultDto<TrainingCenterPlanItemDto>> GetListAsync(
        CenterPlanItemGetListInput input)
    {
        var query = await CreateFilteredQueryAsync(input);
        var totalCount = await AsyncExecuter.CountAsync(query);

        query = ApplySorting(query, input);
        query = ApplyPaging(query, input);

        var entities = await AsyncExecuter.ToListAsync(query);

        // Batch load tenant course names
        var tenantCourseIds = entities.Select(e => e.TenantCourseId).Distinct().ToList();
        var tenantCourses = (await tenantCourseRepository.WithDetailsAsync(c=>c.CatalogCourse)).Where(
            x => tenantCourseIds.Contains(x.Id));
        var courseMap = tenantCourses.ToDictionary(c => c.Id);

        // Batch load units
        var itemIds = entities.Select(e => e.Id).ToList();
        var allUnits = await unitRepository.GetListAsync(x => itemIds.Contains(x.PlanItemId));
        var unitMap = allUnits.GroupBy(u => u.PlanItemId)
            .ToDictionary(g => g.Key, g => g.Select(u => u.UnitId).ToList());

        var dtos = entities.Select(entity =>
        {
            var dto = _toDtoMapper.Map(entity);
            if (courseMap.TryGetValue(entity.TenantCourseId, out var course))
            {
                dto.TenantCourseName =  course.CatalogCourse.CourseNameAr;
            }
            dto.UnitIds = unitMap.GetValueOrDefault(entity.Id, []);
            return dto;
        }).ToList();

        return new PagedResultDto<TrainingCenterPlanItemDto>(totalCount, dtos);
    }

    [Authorize(TrainingPermissions.CenterPlanItems.Create)]
    public override async Task<TrainingCenterPlanItemDto> CreateAsync(
        CreateUpdateCenterPlanItemDto input)
    {
        var plan = await planRepository.GetAsync(input.PlanId);
        if (plan.Status != CenterPlanStatus.Draft)
        {
            throw new BusinessException("Training:CenterPlanItem:PlanNotDraft");
        }

        var entity = _toEntityMapper.Map(input);

        var existingItems = await Repository.GetListAsync(x => x.PlanId == input.PlanId);
        entity.BatchNumber = existingItems
            .Count(i => i.EstimatedStartDate <= entity.EstimatedStartDate) + 1;

        await Repository.InsertAsync(entity, autoSave: true);
        return await BuildItemDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CenterPlanItems.Edit)]
    public override async Task<TrainingCenterPlanItemDto> UpdateAsync(
        Guid id, CreateUpdateCenterPlanItemDto input)
    {
        var entity = await Repository.GetAsync(id);

        var plan = await planRepository.GetAsync(entity.PlanId);
        if (plan.Status != CenterPlanStatus.Draft)
        {
            throw new BusinessException("Training:CenterPlanItem:PlanNotDraft");
        }

        _toEntityMapper.Map(input, entity);
        await Repository.UpdateAsync(entity, autoSave: true);
        return await BuildItemDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CenterPlanItems.Delete)]
    public override async Task DeleteAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        var plan = await planRepository.GetAsync(entity.PlanId);
        if (plan.Status != CenterPlanStatus.Draft)
        {
            throw new BusinessException("Training:CenterPlanItem:PlanNotDraft");
        }

        await unitRepository.DeleteAsync(x => x.PlanItemId == id);
        await base.DeleteAsync(id);
    }

    [Authorize(TrainingPermissions.CenterPlanItems.SetUnits)]
    public async Task<TrainingCenterPlanItemDto> SetUnitsAsync(
        Guid id, SetPlanItemUnitsDto input)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.BeneficiaryType != BeneficiaryType.Internal)
        {
            throw new BusinessException("Training:CenterPlanItem:UnitsOnlyForInternal");
        }

        await unitRepository.DeleteAsync(x => x.PlanItemId == id);

        var units = input.UnitIds.Select(unitId => new TrainingCenterPlanItemUnit
        {
            PlanItemId = id,
            UnitId = unitId
        }).ToList();

        await unitRepository.InsertManyAsync(units, autoSave: true);

        return await BuildItemDtoAsync(entity);
    }

    public override async Task<TrainingCenterPlanItemDto> GetAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        return await BuildItemDtoAsync(entity);
    }

    private async Task<TrainingCenterPlanItemDto> BuildItemDtoAsync(
      TrainingCenterPlanItem entity)
    {
        var dto = _toDtoMapper.Map(entity);

        var course = (await tenantCourseRepository.WithDetailsAsync(c=>c.CatalogCourse)).FirstOrDefault(c=>c.Id== entity.TenantCourseId);
        dto.TenantCourseName = course?.CatalogCourse?.CourseNameAr ?? string.Empty;

        var units = await unitRepository.GetListAsync(x => x.PlanItemId == entity.Id);
        dto.UnitIds = units.Select(u => u.UnitId).ToList();

        return dto;
    }

    protected override TrainingCenterPlanItemDto MapToGetOutputDto(TrainingCenterPlanItem entity)
        => _toDtoMapper.Map(entity);

    protected override TrainingCenterPlanItemDto MapToGetListOutputDto(TrainingCenterPlanItem entity)
        => _toDtoMapper.Map(entity);
}
