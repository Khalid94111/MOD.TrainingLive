using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.AnnualPlanSessions.Dtos;
using MOD.Training.Training.Catalog;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Hr;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Nominations;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans;
using MOD.Training.Training.Plans.Dtos;
using MOD.Training.Training.TenantCourses;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.AnnualPlanSessions;

// Phase 4C-α (v4.10.0) — AnnualPlanSessionAppService.
//
// Five endpoints, all under the AnnualPlanSessions permission group:
//   GetPlanItemsQueueAsync      — PAGE A queue: THApproved-plan items, not yet bound to an
//                                 active session, sorted by Priority DESC + overdue-first
//                                 + PreferredQuarter ASC + CreationTime ASC.
//   CreateInternalSessionAsync  — PAGE B-1: Internal courses, dates required at creation,
//                                 status born Scheduled.
//   CreateExternalSessionAsync  — PAGE B-2: External courses, no dates at creation, status
//                                 born Planned (dates land at SelectPriceQuote in step 5).
//   GetAvailableSubstitutesAsync — fills the substitution dialog: same-rank-as-original,
//                                  active, in tenant, not already nominated on this plan item.
//   GetProgressDashboardAsync   — PAGE E: per-year counts + per-quarter / per-unit progress
//                                 + alerts (overdue plan items, stuck-Planned sessions).
//
// CourseSession execution transitions (SelectPriceQuote, MarkInProgress, MarkCompleted,
// Cancel) live on CourseSessionAppService (step 5) so they apply uniformly to both annual-plan
// and (future Phase 4C-β) center-plan sourced sessions.
[Authorize(AnnualPlanSessionPermissions.Default)]
public class AnnualPlanSessionAppService(
    IRepository<TrainingPlan, Guid> planRepo,
    IRepository<TrainingPlanItem, Guid> planItemRepo,
    IRepository<CourseSession, Guid> sessionRepo,
    IRepository<SessionNomination, Guid> sessionNominationRepo,
    IRepository<Nomination, Guid> nominationRepo,
    IRepository<TenantCourse, Guid> tenantCourseRepo,
    IRepository<CourseCatalog, Guid> catalogRepo,
    IRepository<Employee, Guid> employeeRepo,
    IRepository<Rank, Guid> rankRepo,
    SessionCreationValidator validator)
    : ApplicationService, IAnnualPlanSessionAppService
{
    // Threshold (days) after which a Planned external session counts as "stuck" — fed
    // to the dashboard's StuckPlanned alert stream. Local const so it shows up clearly
    // in code review; promote to a setting if Finance asks for it.
    private const int StuckPlannedThresholdDays = 14;

    // ────────────────────────────────────────────────────────────────────
    // 1. Queue
    // ────────────────────────────────────────────────────────────────────
    public async Task<PagedResultDto<PlanItemQueueItemDto>> GetPlanItemsQueueAsync(PlanItemQueueGetListInput input)
    {
        var year = input.Year ?? DateTime.UtcNow.Year;

        // Plan IDs in scope: THApproved plans for the requested year.
        var planQ = await planRepo.GetQueryableAsync();
        var planList = await AsyncExecuter.ToListAsync(
            planQ.Where(p => p.Status == PlanStatus.THApproved && p.Year == year)
                 .Select(p => new { p.Id, p.Year }));
        var planMap = planList.ToDictionary(p => p.Id, p => p.Year);

        if (planMap.Count == 0)
        {
            return new PagedResultDto<PlanItemQueueItemDto>(0, new List<PlanItemQueueItemDto>());
        }

        // Plan items already bound to an active session — Cancelled doesn't block (Q-C).
        var sessionQ = await sessionRepo.GetQueryableAsync();
        var sessionBoundPlanItemIds = await AsyncExecuter.ToListAsync(
            sessionQ.Where(s => s.TrainingPlanItemId.HasValue && s.Status != SessionStatus.Cancelled)
                    .Select(s => s.TrainingPlanItemId!.Value));
        var sessionBoundSet = new HashSet<Guid>(sessionBoundPlanItemIds);

        // Plan items in scope, with optional filters.
        var planIdsInScope = planMap.Keys.ToList();
        var itemQ = await planItemRepo.GetQueryableAsync();
        itemQ = itemQ.Where(x => planIdsInScope.Contains(x.PlanId));
        if (input.Quarter.HasValue)
            itemQ = itemQ.Where(x => x.PreferredQuarter == input.Quarter.Value);
        if (input.Priority.HasValue)
            itemQ = itemQ.Where(x => x.Priority == input.Priority.Value);
        if (input.CourseType.HasValue)
            itemQ = itemQ.Where(x => x.CourseType == input.CourseType.Value);
        if (input.UnitId.HasValue)
            itemQ = itemQ.Where(x => x.UnitId == input.UnitId.Value);

        var allItems = await AsyncExecuter.ToListAsync(itemQ);

        // In-memory exclusion of session-bound plan items.
        var openItems = allItems.Where(x => !sessionBoundSet.Contains(x.Id)).ToList();
        if (openItems.Count == 0)
        {
            return new PagedResultDto<PlanItemQueueItemDto>(0, new List<PlanItemQueueItemDto>());
        }

        // Course-name and nominee-count enrichment in two batched roundtrips.
        var tenantCourseIds = openItems.Select(x => x.TenantCourseId).Distinct().ToList();
        var tenantCourses = await AsyncExecuter.ToListAsync(
            (await tenantCourseRepo.GetQueryableAsync()).Where(tc => tenantCourseIds.Contains(tc.Id)));
        var catalogIds = tenantCourses.Select(tc => tc.CatalogCourseId).Distinct().ToList();
        var catalogs = await AsyncExecuter.ToListAsync(
            (await catalogRepo.GetQueryableAsync()).Where(c => catalogIds.Contains(c.Id)));
        var tcMap = tenantCourses.ToDictionary(tc => tc.Id);
        var catalogMap = catalogs.ToDictionary(c => c.Id);

        var openItemIds = openItems.Select(x => x.Id).ToList();
        var nomCountList = await AsyncExecuter.ToListAsync(
            (await nominationRepo.GetQueryableAsync())
                .Where(n => openItemIds.Contains(n.PlanItemId))
                .Where(n => n.Status != NominationStatus.Rejected && n.Status != NominationStatus.Returned)
                .GroupBy(n => n.PlanItemId)
                .Select(g => new { PlanItemId = g.Key, Count = g.Count() }));
        var nomCountMap = nomCountList.ToDictionary(x => x.PlanItemId, x => x.Count);

        // Compute IsOverdue per item (year < current OR (year == current AND quarter passed)).
        var now = DateTime.UtcNow;
        var currentQuarter = (now.Month - 1) / 3 + 1;

        var dtos = openItems.Select(x =>
        {
            var planYear = planMap[x.PlanId];
            var quarter = (int)x.PreferredQuarter;
            var isOverdue = planYear < now.Year || (planYear == now.Year && quarter < currentQuarter);

            string nameAr = string.Empty, nameEn = string.Empty;
            if (tcMap.TryGetValue(x.TenantCourseId, out var tc)
                && catalogMap.TryGetValue(tc.CatalogCourseId, out var cat))
            {
                nameAr = cat.CourseNameAr;
                nameEn = cat.CourseNameEn;
            }

            return new PlanItemQueueItemDto
            {
                Id = x.Id,
                PlanId = x.PlanId,
                PlanYear = planYear,
                TenantCourseId = x.TenantCourseId,
                TenantCourseNameAr = nameAr,
                TenantCourseNameEn = nameEn,
                CourseType = x.CourseType,
                PreferredQuarter = x.PreferredQuarter,
                Priority = x.Priority,
                UnitId = x.UnitId,
                NomineesCount = nomCountMap.TryGetValue(x.Id, out var c) ? c : 0,
                IsOverdue = isOverdue,
                CreationTime = x.CreationTime,
            };
        }).ToList();

        // Sort: Priority DESC → overdue first → PreferredQuarter ASC → CreationTime ASC.
        var totalCount = dtos.Count;
        var sorted = dtos
            .OrderByDescending(d => d.Priority)
            .ThenByDescending(d => d.IsOverdue)
            .ThenBy(d => (int)d.PreferredQuarter)
            .ThenBy(d => d.CreationTime)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        return new PagedResultDto<PlanItemQueueItemDto>(totalCount, sorted);
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. Create Internal — born Scheduled with dates
    // ────────────────────────────────────────────────────────────────────
    [Authorize(AnnualPlanSessionPermissions.Create)]
    public async Task<CourseSessionDto> CreateInternalSessionAsync(CreateInternalSessionDto input)
    {
        await validator.ValidateForCreationAsync(input.TrainingPlanItemId);

        var item = await planItemRepo.GetAsync(input.TrainingPlanItemId);
        if (item.CourseType != CourseType.Internal)
            throw new BusinessException("Training:Session:NotInternalCourseType");
        if (input.ActualEndDate < input.ActualStartDate)
            throw new BusinessException("Training:Session:InvalidActualDates");

        foreach (var sub in input.Substitutions)
            await validator.ValidateSubstitutionAsync(sub.OriginalEmployeeId, sub.ReplacementEmployeeId);

        var plan = await planRepo.GetAsync(item.PlanId);

        var session = new CourseSession(GuidGenerator.Create())
        {
            TenantId = CurrentTenant.Id,
            TrainingPlanItemId = item.Id,
            TrainingCenterPlanItemId = null,
            TenantCourseId = item.TenantCourseId,
            CourseType = item.CourseType,
            PreferredQuarter = (int)item.PreferredQuarter,
            PlanYear = plan.Year,
            ActualStartDate = input.ActualStartDate,
            ActualEndDate = input.ActualEndDate,
            Status = SessionStatus.Scheduled,
        };

        await sessionRepo.InsertAsync(session, autoSave: true);
        await CopyNominationsAsync(session.Id, item.Id, input.Substitutions);

        return MapSessionToDto(session);
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. Create External — born Planned, no dates
    // ────────────────────────────────────────────────────────────────────
    [Authorize(AnnualPlanSessionPermissions.Create)]
    public async Task<CourseSessionDto> CreateExternalSessionAsync(CreateExternalSessionDto input)
    {
        await validator.ValidateForCreationAsync(input.TrainingPlanItemId);

        var item = await planItemRepo.GetAsync(input.TrainingPlanItemId);
        if (item.CourseType == CourseType.Internal)
            throw new BusinessException("Training:Session:NotExternalCourseType");

        foreach (var sub in input.Substitutions)
            await validator.ValidateSubstitutionAsync(sub.OriginalEmployeeId, sub.ReplacementEmployeeId);

        var plan = await planRepo.GetAsync(item.PlanId);

        var session = new CourseSession(GuidGenerator.Create())
        {
            TenantId = CurrentTenant.Id,
            TrainingPlanItemId = item.Id,
            TrainingCenterPlanItemId = null,
            TenantCourseId = item.TenantCourseId,
            CourseType = item.CourseType,
            PreferredQuarter = (int)item.PreferredQuarter,
            PlanYear = plan.Year,
            ActualStartDate = null,
            ActualEndDate = null,
            Status = SessionStatus.Planned,
        };

        await sessionRepo.InsertAsync(session, autoSave: true);
        await CopyNominationsAsync(session.Id, item.Id, input.Substitutions);

        return MapSessionToDto(session);
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. Available Substitutes — same-rank-as-original, not already nominated
    // ────────────────────────────────────────────────────────────────────
    [Authorize(AnnualPlanSessionPermissions.Substitute)]
    public async Task<List<AvailableSubstituteDto>> GetAvailableSubstitutesAsync(Guid planItemId, Guid originalEmployeeId)
    {
        var original = await employeeRepo.GetAsync(originalEmployeeId);

        // Already-nominated employees on this plan item (excludes Rejected/Returned).
        var nominatedIds = await AsyncExecuter.ToListAsync(
            (await nominationRepo.GetQueryableAsync())
                .Where(n => n.PlanItemId == planItemId)
                .Where(n => n.Status != NominationStatus.Rejected && n.Status != NominationStatus.Returned)
                .Select(n => n.EmployeeId));
        var excludeSet = new HashSet<Guid>(nominatedIds);

        // Same-rank active employees in scope. Include the original-rank candidates
        // but exclude anyone already on this plan item.
        var candidates = await AsyncExecuter.ToListAsync(
            (await employeeRepo.GetQueryableAsync())
                .Where(e => e.RankId == original.RankId && e.IsActive));

        var rank = await rankRepo.FindAsync(original.RankId);

        return candidates
            .Where(e => !excludeSet.Contains(e.Id))
            .Select(e => new AvailableSubstituteDto
            {
                EmployeeId = e.Id,
                RankId = e.RankId,
                ServiceNumber = e.ServiceNumber,
                FullNameAr = e.FullNameAr,
                FullNameEn = e.FullNameEn,
                RankNameAr = rank?.NameAr,
            })
            .OrderBy(x => x.FullNameAr)
            .ToList();
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. Progress Dashboard
    // ────────────────────────────────────────────────────────────────────
    [Authorize(AnnualPlanSessionPermissions.Dashboard)]
    public async Task<AnnualPlanProgressDto> GetProgressDashboardAsync(int? year)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;

        // Plans in scope.
        var planQ = await planRepo.GetQueryableAsync();
        var planList = await AsyncExecuter.ToListAsync(
            planQ.Where(p => p.Status == PlanStatus.THApproved && p.Year == targetYear)
                 .Select(p => new { p.Id, p.Year }));

        if (planList.Count == 0)
        {
            return new AnnualPlanProgressDto { Year = targetYear };
        }

        var planIds = planList.Select(p => p.Id).ToList();

        // All plan items in scope.
        var allItems = await AsyncExecuter.ToListAsync(
            (await planItemRepo.GetQueryableAsync()).Where(x => planIds.Contains(x.PlanId)));

        // All sessions for those plan items (joined later in-memory).
        var allSessions = await AsyncExecuter.ToListAsync(
            (await sessionRepo.GetQueryableAsync())
                .Where(s => s.TrainingPlanItemId.HasValue
                            && s.PlanYear == targetYear));

        var sessionsByPlanItem = allSessions
            .Where(s => s.TrainingPlanItemId.HasValue)
            .GroupBy(s => s.TrainingPlanItemId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Aggregate counts. A plan item has an "effective" session = the latest non-Cancelled session.
        int planned = 0, scheduled = 0, inProgress = 0, completed = 0, cancelled = 0;
        var now = DateTime.UtcNow;
        var currentQuarter = (now.Month - 1) / 3 + 1;
        var overduePlanItems = new List<TrainingPlanItem>();
        var quarterStats = new Dictionary<int, (int total, int completed, int inProgress, int pending)>
        {
            { 1, (0, 0, 0, 0) }, { 2, (0, 0, 0, 0) }, { 3, (0, 0, 0, 0) }, { 4, (0, 0, 0, 0) }
        };
        // Guid.Empty sentinel represents "no unit" — translated back to null when emitting DTOs.
        var unitStats = new Dictionary<Guid, (int total, int completed)>();

        foreach (var item in allItems)
        {
            var quarter = (int)item.PreferredQuarter;
            var hasActive = false;
            SessionStatus? effectiveStatus = null;

            if (sessionsByPlanItem.TryGetValue(item.Id, out var sessions))
            {
                var active = sessions.Where(s => s.Status != SessionStatus.Cancelled)
                                     .OrderByDescending(s => s.CreationTime)
                                     .FirstOrDefault();
                if (active != null)
                {
                    hasActive = true;
                    effectiveStatus = active.Status;
                    switch (active.Status)
                    {
                        case SessionStatus.Planned: planned++; break;
                        case SessionStatus.Scheduled: scheduled++; break;
                        case SessionStatus.InProgress: inProgress++; break;
                        case SessionStatus.Completed:
                        case SessionStatus.FinanciallyClosed: completed++; break;
                    }
                }
                else
                {
                    cancelled++;
                }
            }

            // Overdue: PreferredQuarter has passed without an active session.
            // Cancelled-only counts as no-session for overdue purposes.
            var isOverdue = quarter < currentQuarter && !hasActive;
            if (isOverdue) overduePlanItems.Add(item);

            // Per-quarter rollup.
            var qStats = quarterStats[quarter];
            qStats.total++;
            if (effectiveStatus is SessionStatus.Completed or SessionStatus.FinanciallyClosed) qStats.completed++;
            else if (effectiveStatus == SessionStatus.InProgress) qStats.inProgress++;
            else qStats.pending++;
            quarterStats[quarter] = qStats;

            // Per-unit rollup. Guid.Empty stands in for "no unit assigned".
            var unitKey = item.UnitId ?? Guid.Empty;
            var uStats = unitStats.GetValueOrDefault(unitKey);
            uStats.total++;
            if (effectiveStatus is SessionStatus.Completed or SessionStatus.FinanciallyClosed) uStats.completed++;
            unitStats[unitKey] = uStats;
        }

        // Stuck-Planned alerts: external sessions in Planned status older than threshold.
        var threshold = now.AddDays(-StuckPlannedThresholdDays);
        var stuck = allSessions
            .Where(s => s.Status == SessionStatus.Planned && s.CreationTime <= threshold)
            .Select(s => new OverdueAlertDto
            {
                Type = "StuckPlanned",
                EntityId = s.Id,
                EntityType = "Session",
                DaysOverdue = (int)(now - s.CreationTime).TotalDays,
                Message = $"Session in Planned status for {(int)(now - s.CreationTime).TotalDays} days",
            })
            .ToList();

        var overdueAlerts = overduePlanItems
            .Select(item =>
            {
                var quartersPast = currentQuarter - (int)item.PreferredQuarter;
                return new OverdueAlertDto
                {
                    Type = "OverduePlanItem",
                    EntityId = item.Id,
                    EntityType = "PlanItem",
                    DaysOverdue = quartersPast * 90,
                    Message = $"Plan item overdue by {quartersPast} quarter(s)",
                };
            })
            .ToList();

        var total = allItems.Count;
        var overallPercent = total == 0 ? 0 : (int)Math.Round(100.0 * completed / total);

        return new AnnualPlanProgressDto
        {
            Year = targetYear,
            TotalPlanItems = total,
            PlannedSessionCount = planned,
            ScheduledSessionCount = scheduled,
            InProgressSessionCount = inProgress,
            CompletedSessionCount = completed,
            CancelledSessionCount = cancelled,
            OverdueCount = overduePlanItems.Count,
            OverallProgressPercent = overallPercent,
            ProgressByQuarter = quarterStats
                .OrderBy(kv => kv.Key)
                .Select(kv => new QuarterProgressDto
                {
                    Quarter = kv.Key,
                    Total = kv.Value.total,
                    Completed = kv.Value.completed,
                    InProgress = kv.Value.inProgress,
                    Pending = kv.Value.pending,
                })
                .ToList(),
            ProgressByUnit = unitStats
                .Select(kv => new UnitProgressDto
                {
                    UnitId = kv.Key == Guid.Empty ? null : kv.Key,
                    UnitName = null, // resolved client-side via OU lookup; step 5/6 may join.
                    Total = kv.Value.total,
                    Completed = kv.Value.completed,
                })
                .ToList(),
            Alerts = overdueAlerts.Concat(stuck)
                .OrderByDescending(a => a.DaysOverdue)
                .ToList(),
        };
    }

    // ────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds SessionNomination rows for a freshly created session by copying the plan item's
    /// approved nominations and replacing any matching original-employee with the substitution
    /// payload. RankId is snapshotted from each final employee so later substitution checks
    /// have a stable rank reference even if HR mutates the employee.
    /// </summary>
    private async Task CopyNominationsAsync(Guid sessionId, Guid planItemId, List<NomineeSubstitutionDto> substitutions)
    {
        var planItemNominations = await AsyncExecuter.ToListAsync(
            (await nominationRepo.GetQueryableAsync())
                .Where(n => n.PlanItemId == planItemId)
                .Where(n => n.Status != NominationStatus.Rejected && n.Status != NominationStatus.Returned));

        if (planItemNominations.Count == 0) return;

        var subByOriginal = substitutions.ToDictionary(s => s.OriginalEmployeeId);

        // Resolve final employee ids (originals + replacements) so we can snapshot their ranks.
        var finalEmployeeIds = planItemNominations
            .Select(n => subByOriginal.TryGetValue(n.EmployeeId, out var sub)
                ? sub.ReplacementEmployeeId
                : n.EmployeeId)
            .Distinct()
            .ToList();

        var employees = await AsyncExecuter.ToListAsync(
            (await employeeRepo.GetQueryableAsync()).Where(e => finalEmployeeIds.Contains(e.Id)));
        var employeeMap = employees.ToDictionary(e => e.Id);

        foreach (var n in planItemNominations)
        {
            var (finalEmployeeId, reason) = subByOriginal.TryGetValue(n.EmployeeId, out var sub)
                ? (sub.ReplacementEmployeeId, sub.Reason)
                : (n.EmployeeId, (string?)null);

            if (!employeeMap.TryGetValue(finalEmployeeId, out var emp))
                throw new BusinessException("Training:Session:SubstituteNotFound")
                    .WithData("EmployeeId", finalEmployeeId);

            var nomination = new SessionNomination(GuidGenerator.Create())
            {
                TenantId = CurrentTenant.Id,
                SessionId = sessionId,
                EmployeeId = finalEmployeeId,
                OriginalEmployeeId = n.EmployeeId,
                RankId = emp.RankId,
                SubstitutionReason = reason,
            };
            await sessionNominationRepo.InsertAsync(nomination, autoSave: true);
        }
    }

    private static CourseSessionDto MapSessionToDto(CourseSession session) => new()
    {
        Id = session.Id,
        TrainingPlanItemId = session.TrainingPlanItemId,
        TrainingCenterPlanItemId = session.TrainingCenterPlanItemId,
        TenantCourseId = session.TenantCourseId,
        CourseType = session.CourseType,
        PreferredQuarter = session.PreferredQuarter,
        PlanYear = session.PlanYear,
        ActualStartDate = session.ActualStartDate,
        ActualEndDate = session.ActualEndDate,
        SelectedPriceQuoteId = session.SelectedPriceQuoteId,
        Status = session.Status,
        // Lean post-create payload: ExecutionStage derived from Status without dependent lookups.
        // The detail page re-fetches via CourseSessionAppService.GetAsync for the full enrichment.
        ExecutionStage = session.CourseType == CourseType.Internal
            ? SessionExecutionStage.AwaitingCompletion
            : SessionExecutionStage.AwaitingQuoteSelection,
        NomineesCount = 0,
        CancellationReason = session.CancellationReason,
        CancelledAt = session.CancelledAt,
        CancelledById = session.CancelledById,
        CreationTime = session.CreationTime,
        CreatorId = session.CreatorId,
        LastModificationTime = session.LastModificationTime,
        LastModifierId = session.LastModifierId,
    };
}
