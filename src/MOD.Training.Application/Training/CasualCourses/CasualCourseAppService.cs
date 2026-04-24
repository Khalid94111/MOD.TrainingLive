using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.CasualCourses.Dtos;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans;
using MOD.Training.Training.Plans.Dtos;
using MOD.Training.Training.TenantCourses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace MOD.Training.Training.CasualCourses;

[Authorize(TrainingPermissions.CasualCourses.Default)]
public class CasualCourseAppService(
    IRepository<CasualCourse, Guid> repository,
    IRepository<CasualCourseFinancial, Guid> financialRepo,
    IRepository<CasualCourseFinancialItemRank, Guid> rankRepo,
    IRepository<CasualCourseNomination, Guid> nominationRepo,
    IRepository<FinancialItem, Guid> financialItemRepo,
    IRepository<PlanNote, Guid> planNoteRepo,
    IOrganizationUnitRepository orgUnitRepository,
    CasualCourseValidator validator,
    NominationConditionValidator conditionValidator,
    FundingScenarioSourceResolver scenarioSourceResolver,
    CasualCourseRankBreakdownManager rankManager,
    EmployeeResolver employeeResolver,
    CourseNameResolver courseNameResolver,
    CasualCourseUnitScope unitScope,
    IPlanNoteAppService planNoteAppService,
    CasualCourseFinancialAppService financialAppService,
    CasualCourseToDtoMapper toDtoMapper,
    CasualCourseFinancialToDtoMapper financialToDtoMapper,
    CasualCourseNominationToDtoMapper nominationToDtoMapper)
    : ApplicationService, ICasualCourseAppService
{
    // ─── READ ──────────────────────────────────────────────────────────

    public async Task<CasualCourseDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessAsync(entity);
        return await BuildDtoAsync(entity);
    }

    public async Task<CasualCourseDetailDto> GetDetailAsync(Guid id)
    {
        var queryable = await repository.WithDetailsAsync(x => x.Nominations!, x => x.Financials!);
        var entity = await AsyncExecuter.FirstOrDefaultAsync(queryable.Where(x => x.Id == id))
            ?? throw new BusinessException("Training:CasualCourse:NotFound");

        await unitScope.EnsureCanAccessAsync(entity);

        var baseDto = await BuildDtoAsync(entity);
        var detail = new CasualCourseDetailDto
        {
            Id = baseDto.Id,
            TenantId = baseDto.TenantId,
            TenantCourseId = baseDto.TenantCourseId,
            UnitId = baseDto.UnitId,
            RequestedById = baseDto.RequestedById,
            CourseType = baseDto.CourseType,
            Priority = baseDto.Priority,
            Justification = baseDto.Justification,
            DescriptionAr = baseDto.DescriptionAr,
            ObjectivesAr = baseDto.ObjectivesAr,
            DurationYears = baseDto.DurationYears,
            DurationMonths = baseDto.DurationMonths,
            DurationDays = baseDto.DurationDays,
            EstimatedDateFrom = baseDto.EstimatedDateFrom,
            EstimatedDateTo = baseDto.EstimatedDateTo,
            FundingSource = baseDto.FundingSource,
            EstimatedTotalCost = baseDto.EstimatedTotalCost,
            FundingScenario = baseDto.FundingScenario,
            SelectedPriceQuoteId = baseDto.SelectedPriceQuoteId,
            Status = baseDto.Status,
            ReturnedFromStatus = baseDto.ReturnedFromStatus,
            IsReturned = baseDto.IsReturned,
            LastReturnNoteId = baseDto.LastReturnNoteId,
            RejectedReason = baseDto.RejectedReason,
            CreationTime = baseDto.CreationTime,
            CreatorId = baseDto.CreatorId,
            LastModificationTime = baseDto.LastModificationTime,
            LastModifierId = baseDto.LastModifierId,
            IsDeleted = baseDto.IsDeleted,
            DeleterId = baseDto.DeleterId,
            DeletionTime = baseDto.DeletionTime,
            CourseNameAr = baseDto.CourseNameAr,
            UnitName = baseDto.UnitName,
            RequesterName = baseDto.RequesterName,
            NomineesCount = baseDto.NomineesCount,
            FinancialsCount = baseDto.FinancialsCount,
            LatestReturnReason = baseDto.LatestReturnReason,
            Financials = (entity.Financials ?? new List<CasualCourseFinancial>())
                .Select(f => financialToDtoMapper.Map(f)).ToList(),
            Nominations = await HydrateNominationsAsync(entity.Nominations ?? new List<CasualCourseNomination>()),
        };

        if (entity.LastReturnNoteId.HasValue)
        {
            var note = await planNoteRepo.FindAsync(entity.LastReturnNoteId.Value);
            if (note != null)
            {
                detail.LatestReturnNote = new PlanNoteDto
                {
                    Id = note.Id,
                    EntityType = note.EntityType,
                    EntityId = note.EntityId,
                    Note = note.Note,
                    AuthorRole = note.AuthorRole,
                    IsReturnReason = note.IsReturnReason,
                    CreationTime = note.CreationTime,
                    CreatorId = note.CreatorId,
                };
            }
        }

        return detail;
    }

    public async Task<PagedResultDto<CasualCourseDto>> GetListAsync(CasualCourseGetListInput input)
    {
        var q = await repository.GetQueryableAsync();
        q = await unitScope.ApplyScopeAsync(q);

        if (input.Status != null && input.Status.Any())
            q = q.Where(x => input.Status.Contains(x.Status));
        if (input.UnitId.HasValue)
            q = q.Where(x => x.UnitId == input.UnitId.Value);
        if (input.IsReturnedOnly)
            q = q.Where(x => x.IsReturned);
        if (input.OnlyMyRequests && CurrentUser.Id.HasValue)
            q = q.Where(x => x.RequestedById == CurrentUser.Id.Value);
        if (input.Year.HasValue)
            q = q.Where(x => x.EstimatedDateFrom.Year == input.Year.Value);

        var totalCount = await AsyncExecuter.CountAsync(q);

        q = q.OrderByDescending(x => x.CreationTime).PageBy(input);
        var entities = await AsyncExecuter.ToListAsync(q);

        var dtos = new List<CasualCourseDto>(entities.Count);
        foreach (var e in entities)
            dtos.Add(await BuildDtoAsync(e));

        return new PagedResultDto<CasualCourseDto>(totalCount, dtos);
    }

    // Patch 4 — GetEstimatePreviewAsync removed. UTM now enters the full breakdown at creation
    // and UGM/TD/TH read the real CasualCourseFinancials; there's no "preview" to serve anymore.

    // ─── CREATE / UPDATE / DELETE ──────────────────────────────────────

    [Authorize(TrainingPermissions.CasualCourses.Create)]
    public async Task<CasualCourseDto> CreateAsync(CreateUpdateCasualCourseDto input)
    {
        if (input.EstimatedDateFrom > input.EstimatedDateTo)
            throw new BusinessException("Training:CasualCourse:DateRangeInvalid");

        var isUnitScoped = await unitScope.IsCurrentUserUnitScopedAsync();
        var resolvedUnitId = isUnitScoped
            ? (await employeeResolver.GetCurrentUserUnitIdAsync()) ?? input.UnitId
            : input.UnitId;

        var entity = new CasualCourse(
            GuidGenerator.Create(),
            input.TenantCourseId,
            resolvedUnitId,
            CurrentUser.Id!.Value,
            input.CourseType,
            input.Priority,
            input.Justification,
            input.DurationDays,
            input.EstimatedDateFrom,
            input.EstimatedDateTo)
        {
            DescriptionAr = input.DescriptionAr,
            ObjectivesAr = input.ObjectivesAr,
            DurationYears = input.DurationYears,
            DurationMonths = input.DurationMonths,
            FundingSource = input.FundingSource,
            Status = CasualCourseStatus.Draft,
        };
        await repository.InsertAsync(entity, autoSave: true);

        foreach (var employeeId in input.NomineeEmployeeIds.Distinct())
        {
            var results = await conditionValidator.ValidateByTenantCourseAsync(
                input.TenantCourseId, employeeId);
            var snapshotJson = JsonSerializer.Serialize(results);
            await nominationRepo.InsertAsync(
                new CasualCourseNomination(GuidGenerator.Create(), entity.Id, employeeId)
                {
                    ConditionSnapshotJson = snapshotJson,
                },
                autoSave: true);
        }

        // Patch 4 — auto-populate the full financial breakdown at creation time.
        // Runs as system (Draft status, no scenario yet — uses FinancialItem placeholder Source).
        if (input.NomineeEmployeeIds.Any())
        {
            await financialAppService.AutoFillInternalAsync(entity.Id, runAsSystem: true);

            if (input.FinancialOverrides != null && input.FinancialOverrides.Count > 0)
                await ApplyUtmFinancialOverridesAsync(entity.Id, input.FinancialOverrides);
        }

        return await BuildDtoAsync(entity);
    }

    private async Task ApplyUtmFinancialOverridesAsync(
        Guid casualCourseId, List<FinancialOverrideDto> overrides)
    {
        var parentQ = await financialRepo.GetQueryableAsync();
        var parents = (await AsyncExecuter.ToListAsync(
                parentQ.Where(x => x.CasualCourseId == casualCourseId)))
            .ToDictionary(p => p.FinancialItemId);

        var rankQ = await rankRepo.GetQueryableAsync();

        foreach (var ov in overrides)
        {
            if (!parents.TryGetValue(ov.FinancialItemId, out var parent)) continue;

            var row = await AsyncExecuter.FirstOrDefaultAsync(
                rankQ.Where(r =>
                    r.CasualCourseFinancialId == parent.Id &&
                    r.RankId == ov.RankId));
            if (row == null) continue;

            await rankManager.UpdateRateAsync(row.Id, ov.RatePerUnitOMR);
        }
    }

    [Authorize(TrainingPermissions.CasualCourses.Edit)]
    public async Task<CasualCourseDto> UpdateAsync(Guid id, CreateUpdateCasualCourseDto input)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessAsync(entity);

        if (entity.Status != CasualCourseStatus.Draft && entity.Status != CasualCourseStatus.ReturnedToCreator)
            throw new BusinessException("Training:CasualCourse:CannotEditInThisStatus");

        var priorNomineeIdsQ = await nominationRepo.GetQueryableAsync();
        var priorNomineeIds = (await AsyncExecuter.ToListAsync(
                priorNomineeIdsQ.Where(n => n.CasualCourseId == entity.Id)))
            .Select(n => n.EmployeeId)
            .ToHashSet();
        var priorDurationDays = entity.DurationDays;

        entity.TenantCourseId = input.TenantCourseId;
        entity.UnitId = input.UnitId;
        entity.CourseType = input.CourseType;
        entity.Priority = input.Priority;
        entity.Justification = input.Justification;
        entity.DescriptionAr = input.DescriptionAr;
        entity.ObjectivesAr = input.ObjectivesAr;
        entity.DurationYears = input.DurationYears;
        entity.DurationMonths = input.DurationMonths;
        entity.DurationDays = input.DurationDays;
        entity.EstimatedDateFrom = input.EstimatedDateFrom;
        entity.EstimatedDateTo = input.EstimatedDateTo;
        entity.FundingSource = input.FundingSource;

        await repository.UpdateAsync(entity, autoSave: true);

        await DiffNomineesAsync(entity, input.NomineeEmployeeIds);

        // Patch 4 — keep rank rows in sync with the nominee list + re-apply per-day formulas
        // when the duration changes. Also top up with any new defaults (e.g. if CourseType flipped).
        var newNomineeIds = input.NomineeEmployeeIds.Distinct().ToHashSet();
        var nomineesChanged = !newNomineeIds.SetEquals(priorNomineeIds);
        var durationChanged = priorDurationDays != entity.DurationDays;

        if (nomineesChanged || durationChanged)
        {
            await rankManager.RefreshForNomineeChangeAsync(entity.Id);
        }

        if (input.FinancialOverrides != null && input.FinancialOverrides.Count > 0)
        {
            await ApplyUtmFinancialOverridesAsync(entity.Id, input.FinancialOverrides);
        }

        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CasualCourses.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessAsync(entity);

        if (entity.Status != CasualCourseStatus.Draft)
            throw new BusinessException("Training:CasualCourse:CannotDeleteInThisStatus");

        await repository.DeleteAsync(id);
    }

    // ─── LIFECYCLE TRANSITIONS ─────────────────────────────────────────

    [Authorize(TrainingPermissions.CasualCourses.Submit)]
    public async Task<CasualCourseDto> SubmitAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessAsync(entity);

        await validator.ValidateForSubmitAsync(id);

        var target = entity.Status == CasualCourseStatus.ReturnedToCreator
            && entity.ReturnedFromStatus.HasValue
            ? entity.ReturnedFromStatus.Value
            : CasualCourseStatus.Submitted;

        entity.Status = target;
        entity.IsReturned = false;
        entity.LastReturnNoteId = null;
        entity.ReturnedFromStatus = null;
        await repository.UpdateAsync(entity, autoSave: true);

        // Clear IsReturned on all nominations
        var nomQ = await nominationRepo.GetQueryableAsync();
        var returnedNoms = await AsyncExecuter.ToListAsync(
            nomQ.Where(x => x.CasualCourseId == id && x.IsReturned));
        foreach (var n in returnedNoms)
        {
            n.IsReturned = false;
            n.LastReturnNoteId = null;
            await nominationRepo.UpdateAsync(n, autoSave: true);
        }

        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CasualCourses.Approve)]
    public async Task<CasualCourseDto> UGMApproveAsync(Guid id, CreatePlanNoteDto? note)
    {
        var entity = await repository.GetAsync(id);
        if (entity.Status != CasualCourseStatus.Submitted)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        entity.Status = CasualCourseStatus.UGMApproved;
        await repository.UpdateAsync(entity, autoSave: true);

        await AppendNoteIfPresentAsync(id, note, isReturnReason: false);

        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CasualCourses.Review)]
    public async Task<CasualCourseDto> StartReviewAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        if (entity.Status != CasualCourseStatus.UGMApproved)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        entity.Status = CasualCourseStatus.UnderReview;
        await repository.UpdateAsync(entity, autoSave: true);
        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CasualCourses.Review)]
    public async Task<CasualCourseDto> AssignScenarioAsync(Guid id, AssignScenarioDto input)
    {
        var entity = await repository.GetAsync(id);
        if (entity.Status != CasualCourseStatus.UnderReview)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        // Patch 4 — scenario is now committed on every AssignScenario call, even Commit=false.
        // The picker on PAGE 4.3 fires this with Commit=false on each click.
        entity.FundingScenario = input.FundingScenario;
        await repository.UpdateAsync(entity, autoSave: true);

        // Re-derive Source on every parent (scenario-dependent) from the canonical resolver.
        var parentQ = await financialRepo.GetQueryableAsync();
        var parents = await AsyncExecuter.ToListAsync(parentQ.Where(x => x.CasualCourseId == id));

        if (parents.Count > 0)
        {
            var fiIds = parents.Select(p => p.FinancialItemId).Distinct().ToList();
            var fiQ = await financialItemRepo.GetQueryableAsync();
            var fis = (await AsyncExecuter.ToListAsync(fiQ.Where(x => fiIds.Contains(x.Id))))
                .ToDictionary(x => x.Id);

            foreach (var parent in parents)
            {
                if (fis.TryGetValue(parent.FinancialItemId, out var fi))
                {
                    parent.Source = scenarioSourceResolver.Resolve(input.FundingScenario, fi);
                    await financialRepo.UpdateAsync(parent);
                }
            }
        }

        // Apply targeted Staff adjustments — each mutates one rank row + refreshes parent/course totals.
        if (input.Adjustments != null && input.Adjustments.Count > 0)
        {
            foreach (var adj in input.Adjustments)
            {
                await rankManager.UpdateRateAsync(adj.CasualCourseFinancialItemRankId, adj.NewRatePerUnitOMR);
            }
        }

        // Recompute course total (rankManager.UpdateRateAsync already did this, but recompute here
        // once for the no-adjustment path too — cheap and keeps the invariant obvious).
        await rankManager.RefreshCourseTotalAsync(id);

        // Reload the course — RefreshCourseTotalAsync wrote EstimatedTotalCost on a fresh load.
        entity = await repository.GetAsync(id);

        if (input.Commit)
        {
            if (!entity.EstimatedTotalCost.HasValue || entity.EstimatedTotalCost.Value <= 0)
                throw new BusinessException("Training:CasualCourse:CostRequired");
            entity.Status = CasualCourseStatus.StaffReviewed;
            await repository.UpdateAsync(entity, autoSave: true);
        }

        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CasualCourses.TDApprove)]
    public async Task<CasualCourseDto> TDApproveAsync(Guid id, CreatePlanNoteDto? note)
    {
        await validator.ValidateForTDApprovalAsync(id);

        var entity = await repository.GetAsync(id);
        entity.Status = CasualCourseStatus.TDApproved;
        await repository.UpdateAsync(entity, autoSave: true);

        await AppendNoteIfPresentAsync(id, note, isReturnReason: false);

        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CasualCourses.HeadApprove)]
    public async Task<CasualCourseDto> HeadApproveAsync(Guid id, CreatePlanNoteDto? note)
    {
        await validator.ValidateForHeadApprovalAsync(id);

        var entity = await repository.GetAsync(id);
        entity.Status = CasualCourseStatus.THApproved;
        await repository.UpdateAsync(entity, autoSave: true);

        await AppendNoteIfPresentAsync(id, note, isReturnReason: false);

        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CasualCourses.Return)]
    public async Task<CasualCourseDto> ReturnAsync(Guid id, ReturnReasonDto input)
    {
        var entity = await repository.GetAsync(id);

        if (entity.Status != CasualCourseStatus.Submitted &&
            entity.Status != CasualCourseStatus.UGMApproved &&
            entity.Status != CasualCourseStatus.UnderReview &&
            entity.Status != CasualCourseStatus.StaffReviewed &&
            entity.Status != CasualCourseStatus.TDApproved)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        entity.ReturnedFromStatus = entity.Status;

        var noteDto = await planNoteAppService.CreateAsync(new CreatePlanNoteDto
        {
            EntityType = PlanNoteEntityType.CasualCourse,
            EntityId = id,
            Note = input.Reason,
            IsReturnReason = true,
        });

        entity.IsReturned = true;
        entity.LastReturnNoteId = noteDto.Id;
        entity.Status = CasualCourseStatus.ReturnedToCreator;
        await repository.UpdateAsync(entity, autoSave: true);

        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CasualCourses.Submit)]
    public async Task<CasualCourseDto> ResubmitAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessAsync(entity);

        if (entity.Status != CasualCourseStatus.ReturnedToCreator)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        // Patch 4 — decision Q3 locked: no auto-fill on resubmit.
        // UTM's edited rates (after return) must be preserved; AutoFillFromDefaultsAsync is only
        // additive so a stray invocation would be harmless, but we keep it explicit for clarity.
        await validator.ValidateForSubmitAsync(id);

        var target = entity.ReturnedFromStatus ?? CasualCourseStatus.Submitted;

        entity.Status = target;
        entity.IsReturned = false;
        entity.LastReturnNoteId = null;
        entity.ReturnedFromStatus = null;
        await repository.UpdateAsync(entity, autoSave: true);

        var nomQ = await nominationRepo.GetQueryableAsync();
        var returnedNoms = await AsyncExecuter.ToListAsync(
            nomQ.Where(x => x.CasualCourseId == id && x.IsReturned));
        foreach (var n in returnedNoms)
        {
            n.IsReturned = false;
            n.LastReturnNoteId = null;
            await nominationRepo.UpdateAsync(n, autoSave: true);
        }

        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CasualCourses.Reject)]
    public async Task<CasualCourseDto> RejectAsync(Guid id, RejectDto input)
    {
        var entity = await repository.GetAsync(id);
        if (entity.Status == CasualCourseStatus.THApproved ||
            entity.Status == CasualCourseStatus.Rejected ||
            entity.Status == CasualCourseStatus.Draft)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        entity.RejectedReason = input.Reason;
        entity.Status = CasualCourseStatus.Rejected;
        await repository.UpdateAsync(entity, autoSave: true);

        await planNoteAppService.CreateAsync(new CreatePlanNoteDto
        {
            EntityType = PlanNoteEntityType.CasualCourse,
            EntityId = id,
            Note = input.Reason,
            IsReturnReason = false,
        });

        return await BuildDtoAsync(entity);
    }

    // ─── PRIVATE HELPERS ───────────────────────────────────────────────

    private async Task DiffNomineesAsync(CasualCourse entity, List<Guid> newNomineeEmployeeIds)
    {
        var requested = newNomineeEmployeeIds.Distinct().ToHashSet();

        var nomQ = await nominationRepo.GetQueryableAsync();
        var existing = await AsyncExecuter.ToListAsync(
            nomQ.Where(x => x.CasualCourseId == entity.Id));
        var existingByEmployee = existing.ToDictionary(x => x.EmployeeId);

        var toRemove = existing.Where(n => !requested.Contains(n.EmployeeId)).ToList();
        var toAdd = requested.Where(id => !existingByEmployee.ContainsKey(id)).ToList();

        foreach (var nom in toRemove)
            await nominationRepo.DeleteAsync(nom);

        foreach (var employeeId in toAdd)
        {
            var results = await conditionValidator.ValidateByTenantCourseAsync(
                entity.TenantCourseId, employeeId);
            var snapshot = JsonSerializer.Serialize(results);
            await nominationRepo.InsertAsync(
                new CasualCourseNomination(GuidGenerator.Create(), entity.Id, employeeId)
                {
                    ConditionSnapshotJson = snapshot,
                },
                autoSave: true);
        }
    }

    private async Task AppendNoteIfPresentAsync(Guid casualCourseId, CreatePlanNoteDto? note, bool isReturnReason)
    {
        if (note == null || string.IsNullOrWhiteSpace(note.Note)) return;

        await planNoteAppService.CreateAsync(new CreatePlanNoteDto
        {
            EntityType = PlanNoteEntityType.CasualCourse,
            EntityId = casualCourseId,
            Note = note.Note,
            IsReturnReason = isReturnReason,
        });
    }

    private async Task<CasualCourseDto> BuildDtoAsync(CasualCourse entity)
    {
        var dto = toDtoMapper.Map(entity);

        var cn = await courseNameResolver.ResolveAsync(entity.TenantCourseId);
        if (cn != null) dto.CourseNameAr = cn.NameAr;

        var ou = await orgUnitRepository.FindAsync(entity.UnitId);
        if (ou != null) dto.UnitName = ou.DisplayName;

        var requester = await employeeResolver.GetByUserIdAsync(entity.RequestedById);
        if (requester != null) dto.RequesterName = requester.FullNameAr;

        var nomQ = await nominationRepo.GetQueryableAsync();
        dto.NomineesCount = await AsyncExecuter.CountAsync(nomQ.Where(x => x.CasualCourseId == entity.Id));

        var finQ = await financialRepo.GetQueryableAsync();
        dto.FinancialsCount = await AsyncExecuter.CountAsync(finQ.Where(x => x.CasualCourseId == entity.Id));

        if (entity.LastReturnNoteId.HasValue)
        {
            var note = await planNoteRepo.FindAsync(entity.LastReturnNoteId.Value);
            dto.LatestReturnReason = note?.Note;
        }

        return dto;
    }

    private async Task<List<CasualCourseNominationDto>> HydrateNominationsAsync(ICollection<CasualCourseNomination> noms)
    {
        var empIds = noms.Select(n => n.EmployeeId).Distinct().ToList();
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
            var dto = nominationToDtoMapper.Map(n);
            if (empMap.TryGetValue(n.EmployeeId, out var emp))
            {
                dto.EmployeeName = emp.FullNameAr;
                dto.RankName = emp.Rank?.NameAr ?? "";
                if (unitLookup.TryGetValue(emp.MainUnitId, out var un))
                    dto.UnitName = un;
            }

            if (!string.IsNullOrWhiteSpace(n.ConditionSnapshotJson))
            {
                try
                {
                    var results = JsonSerializer.Deserialize<List<NominationConditionValidator.ConditionResult>>(n.ConditionSnapshotJson);
                    if (results != null)
                    {
                        dto.ConditionPassed = results.All(r => r.Passed);
                        var failed = results.Where(r => !r.Passed).Select(r => r.Details);
                        dto.ConditionDetails = string.Join(" | ", failed);
                    }
                }
                catch { /* ignore malformed snapshot */ }
            }

            result.Add(dto);
        }
        return result;
    }
}
