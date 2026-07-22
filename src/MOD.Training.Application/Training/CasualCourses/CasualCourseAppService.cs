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
using MOD.Training.Training.Travel.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
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
    IRepository<Rank, Guid> rankRefRepo,
    IRepository<PlanNote, Guid> planNoteRepo,
    IRepository<PriceQuote, Guid> priceQuoteRepo,
    // Execution-stage compute on GetList reads these aggregates per post-approval course.
    IRepository<MOD.Training.Training.Payments.CoursePayment, Guid> coursePaymentRepo,
    IRepository<MOD.Training.Training.Payments.TravelAllowancePayment, Guid> travelAllowanceRepo,
    IOrganizationUnitRepository orgUnitRepository,
    CasualCourseValidator validator,
    FinancialItemDefaultResolver rateResolver,
    PriceQuoteValidator priceQuoteValidator,
    EmployeeResolver employeeResolver,
    CourseNameResolver courseNameResolver,
    CasualCourseUnitScope unitScope,
    IPlanNoteAppService planNoteAppService,
    CasualCourseFinancialItemAppService financialAppService,
    CasualCourseToDtoMapper toDtoMapper,
    CasualCourseFinancialItemToDtoMapper financialToDtoMapper,
    CasualCourseNominationToDtoMapper nominationToDtoMapper,
    ITrainingTravelGateway travelGateway)
    : ApplicationService, ICasualCourseAppService
{
    private static readonly FinancialItemType[] RequiredTravelFinancialItemTypes =
    [
        FinancialItemType.Ticket,
        FinancialItemType.Visa,
        FinancialItemType.Insurance,
        FinancialItemType.Allowance,
        FinancialItemType.Clothing
    ];

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
            ActualStartDate = baseDto.ActualStartDate,
            ActualEndDate = baseDto.ActualEndDate,
            ExecutionStatus = baseDto.ExecutionStatus,
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
            FundingScenarioLabel = ResolveFundingScenarioLabel(baseDto.FundingScenario, baseDto.CourseType),
        };

        // Info Bar (Patch 1): officer/enlisted breakdown via Rank.PersonnelType.
        var noms = entity.Nominations ?? new List<CasualCourseNomination>();
        if (noms.Count > 0)
        {
            var empIds = noms.Select(n => n.EmployeeId).Distinct().ToList();
            var empMap = await employeeResolver.BatchResolveByIdsAsync(empIds);
            foreach (var n in noms)
            {
                if (!empMap.TryGetValue(n.EmployeeId, out var emp) || emp.Rank == null) continue;
                if (string.Equals(emp.Rank.PersonnelType, "Officer", StringComparison.OrdinalIgnoreCase))
                    detail.OfficersCount++;
                else if (string.Equals(emp.Rank.PersonnelType, "Enlisted", StringComparison.OrdinalIgnoreCase))
                    detail.EnlistedCount++;
            }
        }

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

        await PopulateExecutionStagesAsync(entities, dtos);

        // Apply execution-stage filter post-compute (stage is derived, not a DB column).
        if (input.ExecutionStage.HasValue)
        {
            var filtered = dtos.Where(d => d.ExecutionStage == input.ExecutionStage.Value).ToList();
            return new PagedResultDto<CasualCourseDto>(filtered.Count, filtered);
        }

        return new PagedResultDto<CasualCourseDto>(totalCount, dtos);
    }

    // ─── EXECUTION STAGE (Patch 2) ─────────────────────────────────────
    // Computed once per GetList by batching the child-entity lookups for the
    // THApproved subset. Mirrors the documented post-TH lifecycle:
    //   International: quote → Travel → imported allowances → course payment → reallocations
    //   Local: quote → course payment → reallocations
    //   Internal: ready immediately (no quote, travel, payment, or reallocation)

    private async Task PopulateExecutionStagesAsync(
        IReadOnlyList<CasualCourse> entities,
        IReadOnlyList<CasualCourseDto> dtos)
    {
        var thApprovedIds = entities
            .Where(e => e.Status == CasualCourseStatus.THApproved)
            .Select(e => e.Id)
            .ToList();
        if (thApprovedIds.Count == 0) return;

        var travelCompletedByCourseId = new Dictionary<Guid, bool>();
        foreach (var course in entities.Where(x =>
                     x.Status == CasualCourseStatus.THApproved
                     && x.CourseType == CourseType.ExternalInternational))
        {
            try
            {
                var travelResult = await travelGateway.GetByTrainingCourseAsync(course.Id);
                travelCompletedByCourseId[course.Id] = travelResult.IsCompleted;
            }
            catch
            {
                travelCompletedByCourseId[course.Id] = false;
            }
        }

        var cpQ = await coursePaymentRepo.GetQueryableAsync();
        var coursePayments = await AsyncExecuter.ToListAsync(
            cpQ.Where(x => x.CasualCourseId.HasValue && thApprovedIds.Contains(x.CasualCourseId.Value)));
        var coursePaymentByCourseId = coursePayments.ToDictionary(p => p.CasualCourseId!.Value);

        var taQ = await travelAllowanceRepo.GetQueryableAsync();
        var travelAllowances = await AsyncExecuter.ToListAsync(
            taQ.Where(x => x.CasualCourseId.HasValue && thApprovedIds.Contains(x.CasualCourseId.Value)));
        var allowancesByCourseId = travelAllowances
            .GroupBy(x => x.CasualCourseId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Walk dtos and entities in parallel — same order, same length.
        for (var i = 0; i < dtos.Count; i++)
        {
            var entity = entities[i];
            if (entity.Status != CasualCourseStatus.THApproved) continue;

            var dto = dtos[i];
            var travelCompleted = travelCompletedByCourseId.GetValueOrDefault(entity.Id);
            coursePaymentByCourseId.TryGetValue(entity.Id, out var payment);
            allowancesByCourseId.TryGetValue(entity.Id, out var allowances);
            var nomineesCount = dto.NomineesCount;
            var (stage, current, total) = ComputeExecutionStage(
                entity, travelCompleted, payment, allowances, nomineesCount);
            dto.ExecutionStage = stage;
            dto.ExecutionStageProgressCurrent = current;
            dto.ExecutionStageProgressTotal = total;
        }
    }

    /// <summary>
    /// Pure function over already-loaded child collections — no DB calls. Returns
    /// (stage, progressCurrent, progressTotal). Progress fields are null except for
    /// the two stages that carry partial-completion semantics.
    /// </summary>
    private static (ExecutionStage stage, int? current, int? total) ComputeExecutionStage(
        CasualCourse course,
        bool travelCompleted,
        MOD.Training.Training.Payments.CoursePayment? payment,
        List<MOD.Training.Training.Payments.TravelAllowancePayment>? allowances,
        int nomineesCount)
    {
        if (course.ExecutionStatus == SessionStatus.Completed
            || course.ExecutionStatus == SessionStatus.FinanciallyClosed)
        {
            return (ExecutionStage.Completed, null, null);
        }

        if (course.ExecutionStatus == SessionStatus.InProgress)
            return (ExecutionStage.InProgress, null, null);

        // Internal courses have no financial execution workflow.
        if (course.CourseType == CourseType.Internal)
        {
            return (ExecutionStage.FinanciallyComplete, null, null);
        }

        // External flow.
        if (course.SelectedPriceQuoteId == null)
            return (ExecutionStage.AwaitingQuoteSelection, null, null);

        // Travel handoff and imported per-nominee allowances apply only to international courses.
        if (course.CourseType == CourseType.ExternalInternational)
        {
            if (!travelCompleted)
                return (ExecutionStage.AwaitingTravelCompletion, null, null);

            var confirmedAllowances = allowances?.Count(p => p.Status == PaymentStatus.Confirmed) ?? 0;
            if (confirmedAllowances < nomineesCount)
                return (ExecutionStage.AwaitingTravelAllowances, confirmedAllowances, nomineesCount);
        }

        if (payment == null || payment.Status != PaymentStatus.Confirmed)
            return (ExecutionStage.AwaitingCoursePayment, null, null);

        // Casual-course travel costs are executed by the Travel module. Training only
        // records the course invoice, so neither supported funding mode creates a
        // budget-reallocation approval step here.
        return (ExecutionStage.FinanciallyComplete, null, null);
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

        // Financial review owns only the course fee. Travel expenses are calculated by
        // the Travel module after approval and are therefore not estimated here.
        var fiQ = await financialItemRepo.GetQueryableAsync();
        var financialItems = await AsyncExecuter.ToListAsync(
            fiQ.Where(x => x.IsActive && x.ItemType == FinancialItemType.CourseCost)
                .OrderBy(x => x.CreationTime)
                .Take(1));

        var items = new List<PreviewItemDto>(financialItems.Count);
        decimal grandTotal = 0m;

        foreach (var fi in financialItems)
        {
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
                    var resolved = await rateResolver.ResolveRateWithSourceAsync(fi.Id, grp.RankId);
                    var rate = resolved.Rate;
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
        validator.ValidateCourseType(input.CourseType);

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
            await nominationRepo.InsertAsync(
                new CasualCourseNomination(GuidGenerator.Create(), entity.Id, employeeId),
                autoSave: true);
        }

        // Financial rows are not created here. CourseCost is the Training-owned amount;
        // Travel calculates its own expenses after approval.

        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CasualCourses.Edit)]
    public async Task<CasualCourseDto> UpdateAsync(Guid id, CreateUpdateCasualCourseDto input)
    {
        validator.ValidateCourseType(input.CourseType);

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

        // No derived financial rows need recomputing. Travel expenses are owned by Travel.

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
        if (entity.CourseType == CourseType.ExternalLocal)
        {
            // Local courses have no Travel handoff, so there is no funding choice.
            entity.FundingScenario = FundingScenario.FundingSourceCoversAll;
            entity.EstimatedTotalCost = entity.CourseCost;
        }
        await repository.UpdateAsync(entity, autoSave: true);
        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CasualCourses.Review)]
    public async Task<CasualCourseDto> AssignScenarioAsync(Guid id, AssignScenarioDto input)
    {
        var entity = await repository.GetAsync(id);
        if (entity.Status != CasualCourseStatus.UnderReview)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        var scenario = entity.CourseType == CourseType.ExternalLocal
            ? FundingScenario.FundingSourceCoversAll
            : input.FundingScenario;
        if (scenario != FundingScenario.FundingSourceCoversAll
            && scenario != FundingScenario.FundingSourceCoversCourse)
        {
            throw new BusinessException("Training:CasualCourse:UnknownFundingScenario");
        }

        // Both supported modes fund the course fee from the source entered on the
        // request. Only the vote codes sent to Travel differ between the two modes.
        entity.FundingScenario = scenario;
        entity.EstimatedTotalCost = entity.CourseCost;
        await repository.UpdateAsync(entity, autoSave: true);

        // Commit if requested.
        if (input.Commit)
        {
            if (!entity.EstimatedTotalCost.HasValue || entity.EstimatedTotalCost.Value <= 0)
                throw new BusinessException("Training:CasualCourse:CostRequired");

            if (entity.CourseType == CourseType.ExternalInternational)
            {
                if (scenario == FundingScenario.FundingSourceCoversAll
                    && string.IsNullOrWhiteSpace(entity.FundingSourceVoteCode))
                {
                    throw new BusinessException("Training:CasualCourse:FundingSourceVoteCodeRequired");
                }

                if (scenario == FundingScenario.FundingSourceCoversCourse)
                {
                    var travelItemQuery = await financialItemRepo.GetQueryableAsync();
                    var configuredTypes = (await AsyncExecuter.ToListAsync(travelItemQuery.Where(x =>
                            x.IsActive
                            && x.ItemType.HasValue
                            && RequiredTravelFinancialItemTypes.Contains(x.ItemType.Value)
                            && !string.IsNullOrWhiteSpace(x.VoteCode))))
                        .Select(x => x.ItemType!.Value)
                        .Distinct()
                        .ToHashSet();
                    var missingTypes = RequiredTravelFinancialItemTypes
                        .Where(type => !configuredTypes.Contains(type))
                        .ToList();
                    if (missingTypes.Count > 0)
                    {
                        throw new UserFriendlyException(string.Join(
                            Environment.NewLine,
                            missingTypes.Select(type => L["Training:SessionTravel:VoteCodeRequired", type.ToString()])));
                    }
                }
            }

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
        if (entity.CourseType == CourseType.Internal)
        {
            entity.ActualStartDate = entity.EstimatedDateFrom;
            entity.ActualEndDate = entity.EstimatedDateTo;
            entity.ExecutionStatus = SessionStatus.Scheduled;
        }
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
        // UTM's edited rates (after return) must be preserved; the CourseTypeFinancialItemDefaults
        // feature has been removed, so auto-fill is no longer performed.
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

    // ─── PRE-EXECUTION (Phase 4B-α) ─────────────────────────────────────

    [Authorize(TrainingExecutionPermissions.PriceQuotes.Select)]
    public async Task<CasualCourseDto> SelectPriceQuoteAsync(Guid id, SelectPriceQuoteDto input)
    {
        var entity = await repository.GetAsync(id);

        if (entity.Status != CasualCourseStatus.THApproved)
            throw new BusinessException("Training:CasualCourse:NotApprovedYet");
        if (entity.SelectedPriceQuoteId.HasValue)
            throw new BusinessException("Training:PriceQuote:SessionQuotesLocked");

        if (input.ActualEndDate < input.ActualStartDate)
            throw new BusinessException("Training:CasualCourse:InvalidActualDates");

        await priceQuoteValidator.ValidateForSelectionAsync(id, input.PriceQuoteId);

        // Flip IsSelected on the previous winner (if any).
        if (entity.SelectedPriceQuoteId.HasValue &&
            entity.SelectedPriceQuoteId.Value != input.PriceQuoteId)
        {
            var oldQuote = await priceQuoteRepo.FindAsync(entity.SelectedPriceQuoteId.Value);
            if (oldQuote != null && oldQuote.IsSelected)
            {
                oldQuote.IsSelected = false;
                await priceQuoteRepo.UpdateAsync(oldQuote);
            }
        }

        var newQuote = await priceQuoteRepo.GetAsync(input.PriceQuoteId);
        newQuote.IsSelected = true;
        if (newQuote.TotalPrice.HasValue && newQuote.TotalPrice.Value > 0)
            newQuote.QuotedPriceOMR = newQuote.TotalPrice.Value;
        await priceQuoteRepo.UpdateAsync(newQuote);

        entity.SelectedPriceQuoteId = input.PriceQuoteId;
        entity.ActualStartDate = input.ActualStartDate;
        entity.ActualEndDate = input.ActualEndDate;
        entity.ExecutionStatus = SessionStatus.Scheduled;

        await repository.UpdateAsync(entity, autoSave: true);

        return await BuildDtoAsync(entity);
    }

    // ─── EXECUTION ─────────────────────────────────────────────────────

    [Authorize(TrainingPermissions.CourseSession.MarkInProgress)]
    public async Task<CasualCourseDto> MarkInProgressAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessAsync(entity);

        if (entity.Status != CasualCourseStatus.THApproved
            || entity.ExecutionStatus != SessionStatus.Scheduled)
        {
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");
        }

        if (!await IsReadyForExecutionAsync(entity))
            throw new BusinessException("Training:CasualCourse:CompletePreviousStages");

        entity.ExecutionStatus = SessionStatus.InProgress;
        await repository.UpdateAsync(entity, autoSave: true);
        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPermissions.CourseSession.MarkCompleted)]
    public async Task<CasualCourseDto> MarkCompletedAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        await unitScope.EnsureCanAccessAsync(entity);

        if (entity.Status != CasualCourseStatus.THApproved
            || entity.ExecutionStatus != SessionStatus.InProgress)
        {
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");
        }

        entity.ExecutionStatus = SessionStatus.Completed;
        await repository.UpdateAsync(entity, autoSave: true);
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
            await nominationRepo.InsertAsync(
                new CasualCourseNomination(GuidGenerator.Create(), entity.Id, employeeId),
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

    private async Task<bool> IsReadyForExecutionAsync(CasualCourse entity)
    {
        if (entity.CourseType == CourseType.Internal)
            return true;

        if (!entity.SelectedPriceQuoteId.HasValue)
            return false;

        if (entity.CourseType == CourseType.ExternalInternational)
        {
            try
            {
                var travel = await travelGateway.GetByTrainingCourseAsync(entity.Id);
                if (!travel.IsCompleted) return false;
            }
            catch
            {
                return false;
            }

            var nomineeQuery = await nominationRepo.GetQueryableAsync();
            var nomineesCount = await AsyncExecuter.CountAsync(
                nomineeQuery.Where(x => x.CasualCourseId == entity.Id));
            if (nomineesCount == 0) return false;

            var allowanceQuery = await travelAllowanceRepo.GetQueryableAsync();
            var confirmedAllowances = await AsyncExecuter.CountAsync(allowanceQuery.Where(x =>
                x.CasualCourseId == entity.Id && x.Status == PaymentStatus.Confirmed));
            if (confirmedAllowances < nomineesCount) return false;
        }

        var paymentQuery = await coursePaymentRepo.GetQueryableAsync();
        return await AsyncExecuter.AnyAsync(paymentQuery.Where(x =>
            x.CasualCourseId == entity.Id && x.Status == PaymentStatus.Confirmed));
    }

    // Patch 1 (v4.10.1) — short Arabic label for the Info Bar; matches the wording used
    // in the funding-scenario picker dialog. Returns null when no scenario has been chosen yet.
    private static string? ResolveFundingScenarioLabel(
        FundingScenario? scenario,
        CourseType courseType)
    {
        if (courseType == CourseType.ExternalLocal
            && scenario == FundingScenario.FundingSourceCoversAll)
        {
            return "رسوم الدورة من مصدر التمويل";
        }

        return scenario switch
        {
            FundingScenario.FundingSourceCoversAll      => "مصدر التمويل يغطي الدورة والسفر",
            FundingScenario.FundingSourceCoversCourse   => "مصدر التمويل يغطي رسوم الدورة فقط",
            FundingScenario.FinancialItemsCoverAll      => "سيناريو قديم",
            _ => null,
        };
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
                dto.ServiceNumber = emp.ServiceNumber;
                dto.EmployeeName = emp.FullNameAr;
                dto.RankName = emp.Rank?.NameAr ?? "";
                if (unitLookup.TryGetValue(emp.MainUnitId, out var un))
                    dto.UnitName = un;
            }

            result.Add(dto);
        }
        return result;
    }
}
