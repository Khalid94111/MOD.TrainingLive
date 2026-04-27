using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.CasualCourses.Dtos;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Hr;
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
    IRepository<CasualCourseFinancialItem, Guid> financialRepo,
    IRepository<CasualCourseFinancialItemRank, Guid> rankRepo,
    IRepository<CasualCourseNomination, Guid> nominationRepo,
    IRepository<FinancialItem, Guid> financialItemRepo,
    IRepository<CourseTypeFinancialItemDefault, Guid> defaultsRepo,
    IRepository<Rank, Guid> rankRefRepo,
    IRepository<PlanNote, Guid> planNoteRepo,
    IOrganizationUnitRepository orgUnitRepository,
    CasualCourseValidator validator,
    NominationConditionValidator conditionValidator,
    FundingScenarioSourceResolver scenarioSourceResolver,
    FinancialItemDefaultResolver rateResolver,
    CasualCourseRankBreakdownManager rankManager,
    EmployeeResolver employeeResolver,
    CourseNameResolver courseNameResolver,
    CasualCourseUnitScope unitScope,
    IPlanNoteAppService planNoteAppService,
    CasualCourseFinancialItemAppService financialAppService,
    CasualCourseToDtoMapper toDtoMapper,
    CasualCourseFinancialItemToDtoMapper financialToDtoMapper,
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
        var queryable = await repository.WithDetailsAsync(x => x.Nominations!, x => x.FinancialItems!);
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
            FundingSourceName = baseDto.FundingSourceName,
            FundingSourceVoteCode = baseDto.FundingSourceVoteCode,
            CourseCost = baseDto.CourseCost,
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
            FinancialItemsCount = baseDto.FinancialItemsCount,
            LatestReturnReason = baseDto.LatestReturnReason,
            FinancialItems = (entity.FinancialItems ?? new List<CasualCourseFinancialItem>())
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

    // ─── CALCULATOR (Patch 5) ──────────────────────────────────────────
    // Server-side projection used by PAGE 4.2 Section E (UTM live updates) and PAGE 4.4
    // UGM variant (static on page load). Pure function over (CourseType, DurationDays,
    // NomineeIds, CourseCost) — no DB writes, no FundingScenario (not picked yet).

    public async Task<CalculatePreviewDto> CalculatePreviewAsync(CalculatePreviewInput input)
    {
        var nominees = await employeeResolver.GetEmployeesWithRanksAsync(input.NomineeEmployeeIds);
        var nomineesByRank = nominees
            .GroupBy(n => n.RankId)
            .Select(g => new
            {
                RankId = g.Key,
                Count = g.Count(),
                RankNameAr = g.First().RankNameAr,
            })
            .ToList();

        var defQ = await defaultsRepo.WithDetailsAsync(x => x.FinancialItem);
        var defaults = await AsyncExecuter.ToListAsync(
            defQ.Where(x => x.CourseType == input.CourseType).OrderBy(x => x.SortOrder));

        var items = new List<PreviewItemDto>(defaults.Count);
        decimal grandTotal = 0m;

        foreach (var def in defaults)
        {
            var fi = def.FinancialItem;
            var effectiveDays = fi.IsPerDay
                ? input.DurationDays + fi.ExtraDaysBefore + fi.ExtraDaysAfter
                : 1;

            var dto = new PreviewItemDto
            {
                FinancialItemId = fi.Id,
                FinancialItemNameAr = fi.NameAr,
                IsPerDay = fi.IsPerDay,
                IsPerNominee = fi.IsPerNominee,
                EffectiveDays = effectiveDays,
                RankBreakdown = fi.IsPerNominee ? new List<PreviewRankRowDto>() : null,
            };

            decimal itemTotal = 0m;

            if (!fi.IsPerNominee)
            {
                decimal rate;
                if (fi.ItemType == FinancialItemType.CourseCost && input.CourseCost.HasValue)
                {
                    rate = input.CourseCost.Value;
                }
                else
                {
                    var resolved = await rateResolver.ResolveRateWithSourceAsync(fi.Id, rankId: null);
                    rate = resolved.Rate;
                }

                itemTotal = rate * effectiveDays;
            }
            else
            {
                foreach (var grp in nomineesByRank)
                {
                    var (rate, _) = await rateResolver.ResolveRateWithSourceAsync(fi.Id, grp.RankId);
                    var subtotal = rate * effectiveDays * grp.Count;
                    itemTotal += subtotal;

                    dto.RankBreakdown!.Add(new PreviewRankRowDto
                    {
                        RankId = grp.RankId,
                        RankNameAr = grp.RankNameAr,
                        NomineeCount = grp.Count,
                        RatePerUnitOMR = rate,
                        SubtotalOMR = subtotal,
                    });
                }
            }

            dto.SubtotalOMR = itemTotal;
            grandTotal += itemTotal;
            items.Add(dto);
        }

        return new CalculatePreviewDto
        {
            Items = items,
            TotalOMR = grandTotal,
            CourseType = input.CourseType,
            ComputedAt = Clock.Now,
        };
    }

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
            FundingSourceName = input.FundingSourceName,
            FundingSourceVoteCode = input.FundingSourceVoteCode,
            CourseCost = input.CourseCost,
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

        // Patch 5 — financial rows are NOT created here. UTM's view is a read-only calculator
        // (POST /calculate-preview); rows materialise when Staff picks a scenario via
        // AssignScenarioAsync. CourseCost on the entity carries UTM's seed value forward.

        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CasualCourses.Edit)]
    public async Task<CasualCourseDto> UpdateAsync(Guid id, CreateUpdateCasualCourseDto input)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessAsync(entity);

        if (entity.Status != CasualCourseStatus.Draft && entity.Status != CasualCourseStatus.ReturnedToCreator)
            throw new BusinessException("Training:CasualCourse:CannotEditInThisStatus");

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
        entity.FundingSourceName = input.FundingSourceName;
        entity.FundingSourceVoteCode = input.FundingSourceVoteCode;
        entity.CourseCost = input.CourseCost;

        await repository.UpdateAsync(entity, autoSave: true);

        await DiffNomineesAsync(entity, input.NomineeEmployeeIds);

        // Patch 5 — no financial rows exist during Draft / ReturnedToCreator (UpdateAsync's
        // only valid statuses), so nothing to recompute on nominee or duration changes. Rows
        // are created later by Staff via AssignScenarioAsync.

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

        // Step 1 — commit the scenario before doing anything that depends on it.
        // (Subsequent picks land here too — the picker on PAGE 4.3 fires with Commit=false
        // on each click; only the final click sets Commit=true to advance status.)
        entity.FundingScenario = input.FundingScenario;
        await repository.UpdateAsync(entity, autoSave: true);

        // Step 2 — first scenario pick → auto-fill creates rows with correct Source from
        // the start (no placeholder phase). UTM's CourseCost flows through as the seed for
        // the course-cost row's rate. Subsequent picks skip this branch and fall through
        // to the re-derive logic below.
        var parentQ = await financialRepo.GetQueryableAsync();
        var hasFinancialItems = await AsyncExecuter.AnyAsync(parentQ.Where(x => x.CasualCourseId == id));

        if (!hasFinancialItems)
        {
            await financialAppService.AutoFillInternalAsync(
                id, runAsSystem: true, courseCostSeed: entity.CourseCost);
        }
        else
        {
            // Step 2b — scenario change on an existing populated course: re-derive Source on
            // every parent. Staff's manual rate edits on rank rows are preserved.
            var parents = await AsyncExecuter.ToListAsync(parentQ.Where(x => x.CasualCourseId == id));
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

        // Step 3 — apply targeted Staff adjustments (each mutates one rank row and refreshes
        // parent + course totals).
        if (input.Adjustments != null && input.Adjustments.Count > 0)
        {
            foreach (var adj in input.Adjustments)
            {
                await rankManager.UpdateRateAsync(adj.CasualCourseFinancialItemRankId, adj.NewRatePerUnitOMR);
            }
        }

        // Step 4 — make sure the course total is up to date for both the auto-fill-only
        // path (no adjustments) and the no-adjustments-on-existing-rows path.
        await rankManager.RefreshCourseTotalAsync(id);

        entity = await repository.GetAsync(id);

        // Step 5 — commit if requested.
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
        dto.FinancialItemsCount = await AsyncExecuter.CountAsync(finQ.Where(x => x.CasualCourseId == entity.Id));

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
