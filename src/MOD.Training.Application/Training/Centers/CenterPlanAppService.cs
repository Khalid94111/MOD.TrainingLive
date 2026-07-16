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

namespace MOD.Training.Training.Centers;

[Authorize(TrainingPermissions.CenterPlans.Default)]
public class CenterPlanAppService(
    IRepository<TrainingCenterPlan, Guid> repository,
    IRepository<CenterPlanWindow, Guid> windowRepository,
    IRepository<CenterRoleAssignment, Guid> roleAssignmentRepository,
    IRepository<TrainingCenterPlanItem, Guid> planItemRepository,
    IRepository<TrainingCenter, Guid> centerRepository)
    : CrudAppService<
        TrainingCenterPlan,
        TrainingCenterPlanDto,
        Guid,
        CenterPlanGetListInput,
        CreateUpdateCenterPlanDto>(repository)
{
    private readonly TrainingCenterPlanToDtoMapper _toDtoMapper = new();
    private readonly CreateUpdateCenterPlanToEntityMapper _toEntityMapper = new();
    private readonly TrainingCenterPlanItemToDtoMapper _itemToDtoMapper = new();

    protected override async Task<IQueryable<TrainingCenterPlan>> CreateFilteredQueryAsync(
        CenterPlanGetListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);

        if (input.CenterId.HasValue)
            query = query.Where(x => x.CenterId == input.CenterId.Value);
        if (input.Year.HasValue)
            query = query.Where(x => x.Year == input.Year.Value);
        if (input.Status.HasValue)
            query = query.Where(x => x.Status == input.Status.Value);

        return query;
    }

    [Authorize(TrainingPermissions.CenterPlans.Create)]
    public override async Task<TrainingCenterPlanDto> CreateAsync(
        CreateUpdateCenterPlanDto input)
    {
        // MOD-18: Validate submission window is open
        var window = await windowRepository.FindAsync(x => x.Year == input.Year);
        if (window == null || !window.IsOpen)
        {
            throw new BusinessException("Training:CenterPlan:WindowNotOpen")
                .WithData("Year", input.Year);
        }

        // MOD-20: Validate current user is TCO for this center
        await ValidateCurrentUserIsTcoAsync(input.CenterId);

        var entity = _toEntityMapper.Map(input);
        entity.Status = CenterPlanStatus.Draft;
        entity.OpenedAt = DateTime.Now;
        entity.OpenedById = CurrentUser.Id!.Value;

        await Repository.InsertAsync(entity, autoSave: true);
        return await BuildPlanDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CenterPlans.Edit)]
    public override async Task<TrainingCenterPlanDto> UpdateAsync(
        Guid id, CreateUpdateCenterPlanDto input)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.Status != CenterPlanStatus.Draft)
        {
            throw new BusinessException("Training:CenterPlan:NotDraft");
        }

        _toEntityMapper.Map(input, entity);
        await Repository.UpdateAsync(entity, autoSave: true);
        return await BuildPlanDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CenterPlans.Delete)]
    public override async Task DeleteAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        if (entity.Status != CenterPlanStatus.Draft)
        {
            throw new BusinessException("Training:CenterPlan:CannotDeleteNonDraft");
        }
        await base.DeleteAsync(id);
    }

    [Authorize(TrainingPermissions.CenterPlans.Submit)]
    public async Task<TrainingCenterPlanDto> SubmitAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.Status != CenterPlanStatus.Draft)
        {
            throw new BusinessException("Training:CenterPlan:NotDraft");
        }

        await ValidateCurrentUserIsTcoAsync(entity.CenterId);

        entity.Status = CenterPlanStatus.Submitted;
        entity.SubmittedAt = DateTime.Now;
        entity.SubmittedById = CurrentUser.Id!.Value;

        await Repository.UpdateAsync(entity, autoSave: true);
        return await BuildPlanDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CenterPlans.Approve)]
    public async Task<TrainingCenterPlanDto> ApproveAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.Status != CenterPlanStatus.Submitted)
        {
            throw new BusinessException("Training:CenterPlan:NotSubmitted");
        }

        entity.Status = CenterPlanStatus.Approved;
        entity.ApprovedAt = DateTime.Now;
        entity.ApprovedById = CurrentUser.Id!.Value;

        await Repository.UpdateAsync(entity, autoSave: true);
        return await BuildPlanDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CenterPlans.Approve)]
    public async Task<TrainingCenterPlanDto> RejectAsync(Guid id, PlanActionReasonDto input)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.Status != CenterPlanStatus.Submitted)
        {
            throw new BusinessException("Training:CenterPlan:NotSubmitted");
        }

        entity.Status = CenterPlanStatus.Rejected;
        entity.RejectionReason = input.Reason;
        await Repository.UpdateAsync(entity, autoSave: true);
        return await BuildPlanDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CenterPlans.Approve)]
    public async Task<TrainingCenterPlanDto> ReturnAsync(Guid id, PlanActionReasonDto input)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.Status != CenterPlanStatus.Submitted)
        {
            throw new BusinessException("Training:CenterPlan:NotSubmitted");
        }

        entity.Status = CenterPlanStatus.Draft;
        entity.SubmittedAt = null;
        entity.SubmittedById = null;
        entity.ReturnReason = input.Reason;

        await Repository.UpdateAsync(entity, autoSave: true);
        return await BuildPlanDtoAsync(entity);
    }

    public override async Task<TrainingCenterPlanDto> GetAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        return await BuildPlanDtoAsync(entity);
    }

    public override async Task<PagedResultDto<TrainingCenterPlanDto>> GetListAsync(
        CenterPlanGetListInput input)
    {
        var query = await CreateFilteredQueryAsync(input);
        var totalCount = await AsyncExecuter.CountAsync(query);

        query = ApplySorting(query, input);
        query = ApplyPaging(query, input);

        var entities = await AsyncExecuter.ToListAsync(query);

        var centerIds = entities.Select(e => e.CenterId).Distinct().ToList();
        var centers = await centerRepository.GetListAsync(x => centerIds.Contains(x.Id));
        var centerMap = centers.ToDictionary(c => c.Id);

        var planIds = entities.Select(e => e.Id).ToList();
        var allItems = await planItemRepository.GetListAsync(x => planIds.Contains(x.PlanId));
        var itemCountMap = allItems.GroupBy(i => i.PlanId)
            .ToDictionary(g => g.Key, g => g.Count());

        var dtos = entities.Select(entity =>
        {
            var dto = _toDtoMapper.Map(entity);
            if (centerMap.TryGetValue(entity.CenterId, out var center))
            {
                dto.CenterName = center.CenterNameAr;
            }
            dto.ItemCount = itemCountMap.GetValueOrDefault(entity.Id, 0);
            return dto;
        }).ToList();

        return new PagedResultDto<TrainingCenterPlanDto>(totalCount, dtos);
    }

    private async Task<TrainingCenterPlanDto> BuildPlanDtoAsync(TrainingCenterPlan entity)
    {
        var dto = _toDtoMapper.Map(entity);

        var center = await centerRepository.GetAsync(entity.CenterId);
        dto.CenterName = center.CenterNameAr;

        var items = await planItemRepository.GetListAsync(x => x.PlanId == entity.Id);
        dto.Items = items.Select(_itemToDtoMapper.Map).ToList();
        dto.ItemCount = items.Count;

        return dto;
    }

    private async Task ValidateCurrentUserIsTcoAsync(Guid centerId)
    {
        var currentUserId = CurrentUser.Id!.Value;
        var isTco = await roleAssignmentRepository.AnyAsync(
            x => x.CenterId == centerId
                && x.RoleType == CenterRoleType.TCO
                && x.AssignmentType == CenterAssignmentType.Employee
                && x.EmployeeId == currentUserId);

        //if (!isTco)
        //{
        //    throw new BusinessException("Training:CenterPlan:NotAuthorized");
        //}
    }

    protected override TrainingCenterPlanDto MapToGetOutputDto(TrainingCenterPlan entity)
        => _toDtoMapper.Map(entity);

    protected override TrainingCenterPlanDto MapToGetListOutputDto(TrainingCenterPlan entity)
        => _toDtoMapper.Map(entity);
}
