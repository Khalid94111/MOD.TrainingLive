using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Centers.Dtos;
using MOD.Training.Training.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Centers;

[Authorize(TrainingPermissions.Centers.ManageWindows)]
public class CenterPlanWindowAppService(
    IRepository<CenterPlanWindow, Guid> repository)
    : CrudAppService<
        CenterPlanWindow,
        CenterPlanWindowDto,
        Guid,
        CenterPlanWindowGetListInput,
        CreateUpdateCenterPlanWindowDto>(repository)
{
    private readonly CenterPlanWindowToDtoMapper _toDtoMapper = new();
    private readonly CreateUpdateCenterPlanWindowToEntityMapper _toEntityMapper = new();

    protected override async Task<IQueryable<CenterPlanWindow>> CreateFilteredQueryAsync(
        CenterPlanWindowGetListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);

        if (input.Year.HasValue)
        {
            query = query.Where(x => x.Year == input.Year.Value);
        }

        return query;
    }

    public override async Task<CenterPlanWindowDto> CreateAsync(
        CreateUpdateCenterPlanWindowDto input)
    {
        var exists = await Repository.AnyAsync(x => x.Year == input.Year);
        if (exists)
        {
            throw new BusinessException("Training:CenterPlanWindow:YearExists")
                .WithData("Year", input.Year);
        }

        if (input.OpenDate >= input.CloseDate)
        {
            throw new BusinessException("Training:CenterPlanWindow:InvalidDates");
        }

        var entity = _toEntityMapper.Map(input);
        entity.OpenedById = CurrentUser.Id!.Value;
        entity.OpenedAt = DateTime.Now;

        await Repository.InsertAsync(entity, autoSave: true);
        return _toDtoMapper.Map(entity);
    }

    public override async Task<CenterPlanWindowDto> UpdateAsync(
        Guid id, CreateUpdateCenterPlanWindowDto input)
    {
        var entity = await Repository.GetAsync(id);

        var duplicateYear = await Repository.AnyAsync(
            x => x.Year == input.Year && x.Id != id);
        if (duplicateYear)
        {
            throw new BusinessException("Training:CenterPlanWindow:YearExists")
                .WithData("Year", input.Year);
        }

        if (input.OpenDate >= input.CloseDate)
        {
            throw new BusinessException("Training:CenterPlanWindow:InvalidDates");
        }

        _toEntityMapper.Map(input, entity);
        await Repository.UpdateAsync(entity, autoSave: true);
        return _toDtoMapper.Map(entity);
    }

    public async Task<CenterPlanWindowDto> CloseAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        if (!entity.IsOpen)
        {
            throw new BusinessException("Training:CenterPlanWindow:AlreadyClosed");
        }

        entity.CloseDate = DateTime.Now.AddSeconds(-1);
        await Repository.UpdateAsync(entity, autoSave: true);
        return _toDtoMapper.Map(entity);
    }

    protected override CenterPlanWindowDto MapToGetOutputDto(CenterPlanWindow entity)
        => _toDtoMapper.Map(entity);

    protected override CenterPlanWindowDto MapToGetListOutputDto(CenterPlanWindow entity)
        => _toDtoMapper.Map(entity);
}
