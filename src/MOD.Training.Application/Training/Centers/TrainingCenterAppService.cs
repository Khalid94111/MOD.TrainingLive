using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Centers.Dtos;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace MOD.Training.Training.Centers;

[Authorize(TrainingPermissions.Centers.Default)]
public class TrainingCenterAppService(
    IRepository<TrainingCenter, Guid> repository,
    IRepository<CenterRoleAssignment, Guid> roleAssignmentRepository,
    IOrganizationUnitRepository orgUnitRepository)
    : CrudAppService<
        TrainingCenter,
        TrainingCenterDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateTrainingCenterDto>(repository)
{
    private readonly TrainingCenterToDtoMapper _toDtoMapper = new();
    private readonly CreateUpdateTrainingCenterToEntityMapper _toEntityMapper = new();
    private readonly CenterRoleAssignmentToDtoMapper _roleAssignmentToDtoMapper = new();

    [Authorize(TrainingPermissions.Centers.Create)]
    public override async Task<TrainingCenterDto> CreateAsync(
        CreateUpdateTrainingCenterDto input)
    {
        var orgUnit = await orgUnitRepository.GetAsync(input.OrgUnitId);

        var exists = await Repository.AnyAsync(x => x.OrgUnitId == input.OrgUnitId);
        if (exists)
        {
            throw new BusinessException("Training:TrainingCenter:OrgUnitAlreadyAssigned")
                .WithData("OrgUnitName", orgUnit.DisplayName);
        }

        var entity = _toEntityMapper.Map(input);
        await Repository.InsertAsync(entity, autoSave: true);

        var dto = _toDtoMapper.Map(entity);
        dto.OrgUnitName = orgUnit.DisplayName;
        return dto;
    }

    [Authorize(TrainingPermissions.Centers.Edit)]
    public override async Task<TrainingCenterDto> UpdateAsync(
        Guid id, CreateUpdateTrainingCenterDto input)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.OrgUnitId != input.OrgUnitId)
        {
            var exists = await Repository.AnyAsync(
                x => x.OrgUnitId == input.OrgUnitId && x.Id != id);
            if (exists)
            {
                throw new BusinessException("Training:TrainingCenter:OrgUnitAlreadyAssigned");
            }
        }

        _toEntityMapper.Map(input, entity);
        await Repository.UpdateAsync(entity, autoSave: true);

        var orgUnit = await orgUnitRepository.GetAsync(entity.OrgUnitId);
        var dto = _toDtoMapper.Map(entity);
        dto.OrgUnitName = orgUnit.DisplayName;
        return dto;
    }

    [Authorize(TrainingPermissions.Centers.Delete)]
    public override Task DeleteAsync(Guid id)
        => base.DeleteAsync(id);

    public override async Task<TrainingCenterDto> GetAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        var orgUnit = await orgUnitRepository.GetAsync(entity.OrgUnitId);

        var dto = _toDtoMapper.Map(entity);
        dto.OrgUnitName = orgUnit.DisplayName;

        var assignments = await roleAssignmentRepository.GetListAsync(
            x => x.CenterId == id);
        dto.RoleAssignments = assignments.Select(_roleAssignmentToDtoMapper.Map).ToList();
        dto.TcoCount = assignments.Count(a => a.RoleType == CenterRoleType.TCO);

        var tcm = assignments.FirstOrDefault(a => a.RoleType == CenterRoleType.TCM);
        dto.TcmName = tcm?.ServiceNumber;

        return dto;
    }

    public override async Task<PagedResultDto<TrainingCenterDto>> GetListAsync(
        PagedAndSortedResultRequestDto input)
    {
        var totalCount = await Repository.CountAsync();
        var entities = await Repository.GetPagedListAsync(
            input.SkipCount, input.MaxResultCount,
            input.Sorting ?? nameof(TrainingCenter.CenterNameAr));

        var orgUnitIds = entities.Select(e => e.OrgUnitId).Distinct().ToList();
        var orgUnits = await orgUnitRepository.GetListAsync(orgUnitIds);
        var orgUnitMap = orgUnits.ToDictionary(o => o.Id, o => o.DisplayName);

        var centerIds = entities.Select(e => e.Id).ToList();
        var allAssignments = await roleAssignmentRepository.GetListAsync(
            x => centerIds.Contains(x.CenterId));

        var dtos = entities.Select(entity =>
        {
            var dto = _toDtoMapper.Map(entity);
            dto.OrgUnitName = orgUnitMap.GetValueOrDefault(entity.OrgUnitId, string.Empty);

            var centerAssignments = allAssignments.Where(a => a.CenterId == entity.Id).ToList();
            dto.TcoCount = centerAssignments.Count(a => a.RoleType == CenterRoleType.TCO);

            var tcm = centerAssignments.FirstOrDefault(a => a.RoleType == CenterRoleType.TCM);
            dto.TcmName = tcm?.ServiceNumber;

            return dto;
        }).ToList();

        return new PagedResultDto<TrainingCenterDto>(totalCount, dtos);
    }

    [Authorize(TrainingPermissions.Centers.ManageRoles)]
    public async Task<List<CenterRoleAssignmentDto>> GetRoleAssignmentsAsync(Guid centerId)
    {
        var assignments = await roleAssignmentRepository.GetListAsync(
            x => x.CenterId == centerId);
        return assignments.Select(_roleAssignmentToDtoMapper.Map).ToList();
    }

    [Authorize(TrainingPermissions.Centers.ManageRoles)]
    public async Task<List<CenterRoleAssignmentDto>> SetRoleAssignmentsAsync(
        Guid centerId, SetCenterRoleAssignmentsDto input)
    {
        await Repository.GetAsync(centerId);

        var tcmCount = input.Assignments.Count(a => a.RoleType == CenterRoleType.TCM);
        if (tcmCount > 1)
        {
            throw new BusinessException("Training:CenterRoleAssignment:MultipleTcm");
        }

        await roleAssignmentRepository.DeleteAsync(x => x.CenterId == centerId);

        var entities = input.Assignments.Select(a => new CenterRoleAssignment
        {
            CenterId = centerId,
            RoleType = a.RoleType,
            AssignmentType = a.AssignmentType,
            EmployeeId = a.EmployeeId,
            ServiceNumber = a.ServiceNumber,
            PositionId = a.PositionId
        }).ToList();

        await roleAssignmentRepository.InsertManyAsync(entities, autoSave: true);

        return entities.Select(_roleAssignmentToDtoMapper.Map).ToList();
    }

    protected override TrainingCenterDto MapToGetOutputDto(TrainingCenter entity)
        => _toDtoMapper.Map(entity);

    protected override TrainingCenterDto MapToGetListOutputDto(TrainingCenter entity)
        => _toDtoMapper.Map(entity);
}
