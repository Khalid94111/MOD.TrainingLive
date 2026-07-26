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
using Volo.Abp.Identity;

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
    IOrganizationUnitRepository orgUnitRepository,
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
        var now = Clock.Now;
        var targetYear = year ?? now.Year;

        // Load the approved-plan years once so the UI can offer valid choices
        // instead of accepting an arbitrary number.
        var planQ = await planRepo.GetQueryableAsync();
        var approvedPlans = await AsyncExecuter.ToListAsync(
            planQ.Where(p => p.Status == PlanStatus.THApproved)
                 .Select(p => new { p.Id, p.Year }));
        var availableYears = approvedPlans
            .Select(x => x.Year)
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();
        var planIds = approvedPlans
            .Where(x => x.Year == targetYear)
            .Select(x => x.Id)
            .ToList();

        if (planIds.Count == 0)
        {
            return new AnnualPlanProgressDto
            {
                Year = targetYear,
                AvailableYears = availableYears
            };
        }

        var allItems = await AsyncExecuter.ToListAsync(
            (await planItemRepo.GetQueryableAsync()).Where(x => planIds.Contains(x.PlanId)));
        var planItemIds = allItems.Select(x => x.Id).ToList();

        // Sessions must belong to an item in the approved plans being measured.
        // PlanYear alone can include sessions from returned or non-approved plans.
        var allSessions = planItemIds.Count == 0
            ? []
            : await AsyncExecuter.ToListAsync(
                (await sessionRepo.GetQueryableAsync())
                    .Where(s => s.TrainingPlanItemId.HasValue
                                && planItemIds.Contains(s.TrainingPlanItemId.Value)));

        var sessionsByPlanItem = allSessions
            .Where(s => s.TrainingPlanItemId.HasValue)
            .GroupBy(s => s.TrainingPlanItemId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Resolve the course and unit names on the server. This keeps the dashboard
        // usable for roles that do not have Identity OrganizationUnit permissions.
        var tenantCourseIds = allItems.Select(x => x.TenantCourseId).Distinct().ToList();
        var tenantCourses = tenantCourseIds.Count == 0
            ? []
            : await AsyncExecuter.ToListAsync(
                (await tenantCourseRepo.GetQueryableAsync())
                    .Where(x => tenantCourseIds.Contains(x.Id)));
        var catalogIds = tenantCourses.Select(x => x.CatalogCourseId).Distinct().ToList();
        var catalogs = catalogIds.Count == 0
            ? []
            : await AsyncExecuter.ToListAsync(
                (await catalogRepo.GetQueryableAsync())
                    .Where(x => catalogIds.Contains(x.Id)));
        var catalogById = catalogs.ToDictionary(x => x.Id);
        var catalogByTenantCourseId = tenantCourses
            .Where(x => catalogById.ContainsKey(x.CatalogCourseId))
            .ToDictionary(x => x.Id, x => catalogById[x.CatalogCourseId]);

        var unitIds = allItems
            .Where(x => x.UnitId.HasValue)
            .Select(x => x.UnitId!.Value)
            .Distinct()
            .ToList();
        var unitNames = new Dictionary<Guid, string>();
        foreach (var unitId in unitIds)
        {
            var unit = await orgUnitRepository.FindAsync(unitId);
            if (unit != null)
            {
                unitNames[unitId] = unit.DisplayName;
            }
        }

        var overallStats = new DashboardStageStats();
        var cancelled = 0;
        var quarterStats = new Dictionary<int, DashboardStageStats>
        {
            { 1, new() }, { 2, new() }, { 3, new() }, { 4, new() }
        };
        var unitStats = new Dictionary<Guid, DashboardStageStats>();
        var activeSessions = new Dictionary<Guid, CourseSession>();
        var alerts = new List<OverdueAlertDto>();

        foreach (var item in allItems)
        {
            var quarter = (int)item.PreferredQuarter;
            CourseSession? activeSession = null;

            if (sessionsByPlanItem.TryGetValue(item.Id, out var sessions))
            {
                activeSession = sessions
                    .Where(s => s.Status != SessionStatus.Cancelled)
                    .OrderByDescending(s => s.CreationTime)
                    .FirstOrDefault();
                if (activeSession == null)
                {
                    cancelled++;
                }
            }

            var effectiveStatus = activeSession?.Status;
            overallStats.Add(effectiveStatus);
            quarterStats[quarter].Add(effectiveStatus);
            var unitKey = item.UnitId ?? Guid.Empty;
            if (!unitStats.TryGetValue(unitKey, out var unitStat))
            {
                unitStat = new DashboardStageStats();
                unitStats[unitKey] = unitStat;
            }
            unitStat.Add(effectiveStatus);

            if (activeSession != null)
            {
                activeSessions[item.Id] = activeSession;
            }

            // A plan item is overdue only after its actual quarter end. This works
            // correctly for the current, past, and future plan years.
            var quarterEnd = GetQuarterEnd(targetYear, quarter);
            if (activeSession == null && now.Date > quarterEnd)
            {
                var course = catalogByTenantCourseId.GetValueOrDefault(item.TenantCourseId);
                alerts.Add(new OverdueAlertDto
                {
                    Type = "OverduePlanItem",
                    EntityId = item.Id,
                    EntityType = "PlanItem",
                    DaysOverdue = (now.Date - quarterEnd).Days,
                    CourseNameAr = course?.CourseNameAr ?? string.Empty,
                    CourseNameEn = course?.CourseNameEn ?? string.Empty,
                    CourseType = item.CourseType,
                    PreferredQuarter = quarter,
                    DueDate = quarterEnd
                });
            }
        }

        // Generate one actionable session alert per active session, in priority order.
        var threshold = now.AddDays(-StuckPlannedThresholdDays);
        foreach (var item in allItems)
        {
            if (!activeSessions.TryGetValue(item.Id, out var session))
            {
                continue;
            }

            var course = catalogByTenantCourseId.GetValueOrDefault(item.TenantCourseId);
            var quarterEnd = GetQuarterEnd(targetYear, (int)item.PreferredQuarter);
            OverdueAlertDto? alert = null;

            if (session.Status == SessionStatus.Planned && session.CreationTime <= threshold)
            {
                alert = new OverdueAlertDto
                {
                    Type = "StuckPlanned",
                    DaysOverdue = (int)(now - session.CreationTime).TotalDays,
                    DueDate = session.CreationTime.AddDays(StuckPlannedThresholdDays)
                };
            }
            else if (session.Status is SessionStatus.Scheduled or SessionStatus.InProgress)
            {
                if (session.ActualEndDate.HasValue && now.Date > session.ActualEndDate.Value.Date)
                {
                    alert = new OverdueAlertDto
                    {
                        Type = "CompletionOverdue",
                        DaysOverdue = (now.Date - session.ActualEndDate.Value.Date).Days,
                        DueDate = session.ActualEndDate.Value.Date
                    };
                }
                else if (session.Status == SessionStatus.Scheduled
                         && session.ActualStartDate.HasValue
                         && now.Date > session.ActualStartDate.Value.Date)
                {
                    alert = new OverdueAlertDto
                    {
                        Type = "ExecutionNotStarted",
                        DaysOverdue = (now.Date - session.ActualStartDate.Value.Date).Days,
                        DueDate = session.ActualStartDate.Value.Date
                    };
                }
                else if (!session.ActualStartDate.HasValue || !session.ActualEndDate.HasValue)
                {
                    alert = new OverdueAlertDto
                    {
                        Type = "ScheduleMissingDates"
                    };
                }
                else if (session.Status == SessionStatus.Scheduled
                         && session.ActualStartDate.Value.Date > quarterEnd)
                {
                    alert = new OverdueAlertDto
                    {
                        Type = "ScheduledAfterQuarter",
                        DaysOverdue = (session.ActualStartDate.Value.Date - quarterEnd).Days,
                        DueDate = quarterEnd
                    };
                }
            }

            if (alert == null)
            {
                continue;
            }

            alert.EntityId = session.Id;
            alert.EntityType = "Session";
            alert.CourseNameAr = course?.CourseNameAr ?? string.Empty;
            alert.CourseNameEn = course?.CourseNameEn ?? string.Empty;
            alert.CourseType = item.CourseType;
            alert.PreferredQuarter = (int)item.PreferredQuarter;
            alerts.Add(alert);
        }

        var total = allItems.Count;
        var overallPercent = total == 0
            ? 0
            : (int)Math.Round(100.0 * overallStats.Completed / total);

        return new AnnualPlanProgressDto
        {
            Year = targetYear,
            AvailableYears = availableYears,
            TotalPlanItems = total,
            AwaitingSessionCount = overallStats.AwaitingSession,
            PlannedSessionCount = overallStats.Planned,
            ScheduledSessionCount = overallStats.Scheduled,
            InProgressSessionCount = overallStats.InProgress,
            CompletedSessionCount = overallStats.Completed,
            CancelledSessionCount = cancelled,
            OverdueCount = alerts.Count(x => x.Type == "OverduePlanItem"),
            AttentionCount = alerts.Count,
            OverallProgressPercent = overallPercent,
            ProgressByQuarter = quarterStats
                .OrderBy(kv => kv.Key)
                .Select(kv => new QuarterProgressDto
                {
                    Quarter = kv.Key,
                    Total = kv.Value.Total,
                    AwaitingSession = kv.Value.AwaitingSession,
                    Planned = kv.Value.Planned,
                    Scheduled = kv.Value.Scheduled,
                    InProgress = kv.Value.InProgress,
                    Completed = kv.Value.Completed,
                    Pending = kv.Value.Total - kv.Value.Completed
                })
                .ToList(),
            ProgressByUnit = unitStats
                .Select(kv => new UnitProgressDto
                {
                    UnitId = kv.Key == Guid.Empty ? null : kv.Key,
                    UnitName = kv.Key == Guid.Empty
                        ? null
                        : unitNames.GetValueOrDefault(kv.Key),
                    Total = kv.Value.Total,
                    AwaitingSession = kv.Value.AwaitingSession,
                    Planned = kv.Value.Planned,
                    Scheduled = kv.Value.Scheduled,
                    InProgress = kv.Value.InProgress,
                    Completed = kv.Value.Completed
                })
                .OrderByDescending(x => x.Total)
                .ThenBy(x => x.UnitName)
                .ToList(),
            Alerts = alerts
                .OrderByDescending(a => a.DaysOverdue)
                .ToList(),
        };
    }

    // ────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────

    private static DateTime GetQuarterEnd(int year, int quarter)
    {
        var month = quarter * 3;
        return new DateTime(year, month, DateTime.DaysInMonth(year, month));
    }

    private sealed class DashboardStageStats
    {
        public int Total { get; private set; }
        public int AwaitingSession { get; private set; }
        public int Planned { get; private set; }
        public int Scheduled { get; private set; }
        public int InProgress { get; private set; }
        public int Completed { get; private set; }

        public void Add(SessionStatus? status)
        {
            Total++;
            switch (status)
            {
                case SessionStatus.Planned:
                    Planned++;
                    break;
                case SessionStatus.Scheduled:
                    Scheduled++;
                    break;
                case SessionStatus.InProgress:
                    InProgress++;
                    break;
                case SessionStatus.Completed:
                case SessionStatus.FinanciallyClosed:
                    Completed++;
                    break;
                default:
                    AwaitingSession++;
                    break;
            }
        }
    }

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
