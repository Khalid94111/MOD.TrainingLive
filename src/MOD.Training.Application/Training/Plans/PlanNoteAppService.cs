using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Plans;

[Authorize(TrainingPermissions.PlanNote.Default)]
public class PlanNoteAppService(
    IRepository<PlanNote, Guid> repository,
    EmployeeResolver employeeResolver,
    PlanNoteToDtoMapper toDtoMapper)
    : ApplicationService, IPlanNoteAppService
{
    [Authorize(TrainingPermissions.PlanNote.Create)]
    public async Task<PlanNoteDto> CreateAsync(CreatePlanNoteDto input)
    {
        var authorRole = await InferAuthorRoleAsync();

        var entity = new PlanNote(
            GuidGenerator.Create(),
            input.EntityType,
            input.EntityId,
            input.Note,
            authorRole,
            input.IsReturnReason);

        await repository.InsertAsync(entity, autoSave: true);
        return toDtoMapper.Map(entity);
    }

    public async Task<List<PlanNoteDto>> GetListAsync(PlanNoteGetListInput input)
    {
        var queryable = await repository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(
            queryable
                .Where(x => x.EntityType == input.EntityType && x.EntityId == input.EntityId)
                .OrderBy(x => x.CreationTime));

        var creatorIds = items
            .Where(x => x.CreatorId.HasValue)
            .Select(x => x.CreatorId!.Value)
            .Distinct().ToList();
        var creators = await employeeResolver.BatchResolveByUserIdsAsync(creatorIds);

        return items.Select(e =>
        {
            var dto = toDtoMapper.Map(e);
            if (e.CreatorId.HasValue && creators.TryGetValue(e.CreatorId.Value, out var emp))
                dto.CreatedByName = emp.FullNameAr;
            return dto;
        }).ToList();
    }

    private async Task<PlanNoteAuthorRole> InferAuthorRoleAsync()
    {
        if (await AuthorizationService.IsGrantedAsync(TrainingPermissions.TrainingPlan.FinalApprove))
            return PlanNoteAuthorRole.TH;
        if (await AuthorizationService.IsGrantedAsync(TrainingPermissions.TrainingPlan.Approve))
            return PlanNoteAuthorRole.TD;
        if (await AuthorizationService.IsGrantedAsync(TrainingPermissions.TrainingPlan.Review))
            return PlanNoteAuthorRole.Staff;
        return PlanNoteAuthorRole.UTM;
    }
}
