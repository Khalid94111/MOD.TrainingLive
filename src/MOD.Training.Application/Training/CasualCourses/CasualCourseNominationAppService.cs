using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.CasualCourses.Dtos;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans;
using MOD.Training.Training.Plans.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace MOD.Training.Training.CasualCourses;

[Authorize(TrainingPermissions.CasualCourses.Default)]
public class CasualCourseNominationAppService(
    IRepository<CasualCourseNomination, Guid> repository,
    IRepository<CasualCourse, Guid> casualCourseRepo,
    IRepository<PlanNote, Guid> planNoteRepo,
    IOrganizationUnitRepository orgUnitRepository,
    EmployeeResolver employeeResolver,
    IPlanNoteAppService planNoteAppService,
    CasualCourseNominationToDtoMapper toDtoMapper)
    : ApplicationService, ICasualCourseNominationAppService
{
    public async Task<List<CasualCourseNominationDto>> GetListByCasualCourseAsync(Guid casualCourseId)
    {
        var q = await repository.GetQueryableAsync();
        var noms = await AsyncExecuter.ToListAsync(q.Where(x => x.CasualCourseId == casualCourseId));

        var empIds = noms.Select(x => x.EmployeeId).Distinct().ToList();
        var empMap = await employeeResolver.BatchResolveByIdsAsync(empIds);

        var unitIds = empMap.Values.Select(e => e.MainUnitId).Distinct().ToList();
        var unitLookup = new Dictionary<Guid, string>();
        foreach (var uid in unitIds)
        {
            var ou = await orgUnitRepository.FindAsync(uid);
            if (ou != null) unitLookup[uid] = ou.DisplayName;
        }

        var result = new List<CasualCourseNominationDto>();
        foreach (var n in noms)
        {
            var dto = toDtoMapper.Map(n);
            if (empMap.TryGetValue(n.EmployeeId, out var emp))
            {
                dto.EmployeeName = emp.FullNameAr;
                dto.RankName = emp.Rank?.NameAr ?? "";
                if (unitLookup.TryGetValue(emp.MainUnitId, out var un))
                    dto.UnitName = un;
            }

            if (n.LastReturnNoteId.HasValue)
            {
                var note = await planNoteRepo.FindAsync(n.LastReturnNoteId.Value);
                dto.LastReturnNote = note?.Note;
            }

            result.Add(dto);
        }
        return result;
    }

    [Authorize(TrainingPermissions.CasualCourses.Edit)]
    public async Task<CasualCourseNominationDto> AddAsync(Guid casualCourseId, Guid employeeId)
    {
        var cc = await casualCourseRepo.GetAsync(casualCourseId);
        if (cc.Status != CasualCourseStatus.Draft && cc.Status != CasualCourseStatus.ReturnedToCreator)
            throw new BusinessException("Training:CasualCourse:CannotEditInThisStatus");

        // Unique check
        var q = await repository.GetQueryableAsync();
        if (await AsyncExecuter.AnyAsync(
                q.Where(x => x.CasualCourseId == casualCourseId && x.EmployeeId == employeeId)))
            throw new BusinessException("Training:CasualCourse:DuplicateNominee");

        var entity = new CasualCourseNomination(GuidGenerator.Create(), casualCourseId, employeeId);
        await repository.InsertAsync(entity, autoSave: true);
        return toDtoMapper.Map(entity);
    }

    [Authorize(TrainingPermissions.CasualCourses.Edit)]
    public async Task RemoveAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        var cc = await casualCourseRepo.GetAsync(entity.CasualCourseId);
        if (cc.Status != CasualCourseStatus.Draft && cc.Status != CasualCourseStatus.ReturnedToCreator)
            throw new BusinessException("Training:CasualCourse:CannotEditInThisStatus");

        await repository.DeleteAsync(id);
    }

    [Authorize(TrainingPermissions.CasualCourses.Return)]
    public async Task<CasualCourseNominationDto> ReturnAsync(Guid id, ReturnReasonDto input)
    {
        var entity = await repository.GetAsync(id);
        var cc = await casualCourseRepo.GetAsync(entity.CasualCourseId);

        if (cc.Status != CasualCourseStatus.UGMApproved &&
            cc.Status != CasualCourseStatus.UnderReview &&
            cc.Status != CasualCourseStatus.StaffReviewed &&
            cc.Status != CasualCourseStatus.TDApproved)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        var noteDto = await planNoteAppService.CreateAsync(new CreatePlanNoteDto
        {
            EntityType = PlanNoteEntityType.CasualCourseNomination,
            EntityId = id,
            Note = input.Reason,
            IsReturnReason = true,
        });

        entity.IsReturned = true;
        entity.LastReturnNoteId = noteDto.Id;
        await repository.UpdateAsync(entity, autoSave: true);

        if (!cc.IsReturned)
        {
            cc.IsReturned = true;
            await casualCourseRepo.UpdateAsync(cc, autoSave: true);
        }

        return toDtoMapper.Map(entity);
    }

    [Authorize(TrainingPermissions.CasualCourses.Edit)]
    public async Task<CasualCourseNominationDto> ReplaceAsync(Guid id, Guid newEmployeeId)
    {
        var entity = await repository.GetAsync(id);
        var cc = await casualCourseRepo.GetAsync(entity.CasualCourseId);
        if (cc.Status != CasualCourseStatus.Draft && cc.Status != CasualCourseStatus.ReturnedToCreator)
            throw new BusinessException("Training:CasualCourse:CannotEditInThisStatus");

        // Uniqueness on new employee
        var q = await repository.GetQueryableAsync();
        if (await AsyncExecuter.AnyAsync(
                q.Where(x => x.CasualCourseId == entity.CasualCourseId &&
                             x.EmployeeId == newEmployeeId &&
                             x.Id != id)))
            throw new BusinessException("Training:CasualCourse:DuplicateNominee");

        entity.EmployeeId = newEmployeeId;
        entity.IsReturned = false;
        entity.LastReturnNoteId = null;
        await repository.UpdateAsync(entity, autoSave: true);

        return toDtoMapper.Map(entity);
    }
}
