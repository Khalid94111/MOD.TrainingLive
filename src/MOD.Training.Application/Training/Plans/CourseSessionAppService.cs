using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Catalog;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Execution;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Hr;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Payments;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans.Dtos;
using MOD.Training.Training.TenantCourses;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace MOD.Training.Training.Plans;

// Phase 4C-α (v4.10.0) — full CourseSessionAppService.
//
// Source-agnostic execution endpoints for sessions, no matter whether they came from an
// annual TrainingPlanItem (Phase 4C-α) or a future TrainingCenterPlanItem (Phase 4C-β).
//
//   GetAsync             — full detail with nominations + selected-quote enrichment
//   GetListAsync         — paged list with computed SessionExecutionStage per row
//   SelectPriceQuoteAsync — Planned → Scheduled, atomic (IsSelected + dates + status)
//   MarkInProgressAsync  — Scheduled → InProgress
//   MarkCompletedAsync   — InProgress → Completed
//   CancelAsync          — (Planned | Scheduled) → Cancelled; no cascading deletes
//
// ExecutionStage filter on GetListAsync is applied in-memory after stage computation,
// since stage is derived rather than stored. The non-stage filters push to the DB.
[Authorize(TrainingPermissions.CourseSession.Default)]
public class CourseSessionAppService(
    IRepository<CourseSession, Guid> sessionRepo,
    IRepository<SessionNomination, Guid> sessionNominationRepo,
    IRepository<TenantCourse, Guid> tenantCourseRepo,
    IRepository<CourseCatalog, Guid> catalogRepo,
    IRepository<Employee, Guid> employeeRepo,
    IRepository<Rank, Guid> rankRepo,
    IRepository<TravelInstruction, Guid> travelInstructionRepo,
    IRepository<TravelAllowancePayment, Guid> travelAllowanceRepo,
    IRepository<CoursePayment, Guid> coursePaymentRepo,
    IRepository<PriceQuote, Guid> priceQuoteRepo,
    IRepository<TrainingProvider, Guid> providerRepo,
    IRepository<TrainingPlanItem, Guid> planItemRepo,
    IOrganizationUnitRepository orgUnitRepository,
    PlanItemCostCalculator planItemCostCalculator,
    PriceQuoteValidator priceQuoteValidator)
    : ApplicationService, ICourseSessionAppService
{
    // ────────────────────────────────────────────────────────────────────
    // GET — detail
    // ────────────────────────────────────────────────────────────────────
    public async Task<CourseSessionDetailDto> GetAsync(Guid id)
    {
        var session = await sessionRepo.GetAsync(id);
        return await BuildDetailDtoAsync(session);
    }

    // ────────────────────────────────────────────────────────────────────
    // GET — paged list with ExecutionStage
    // ────────────────────────────────────────────────────────────────────
    public async Task<PagedResultDto<CourseSessionDto>> GetListAsync(CourseSessionGetListInput input)
    {
        var queryable = await sessionRepo.GetQueryableAsync();

        if (input.TrainingPlanItemId.HasValue)
            queryable = queryable.Where(x => x.TrainingPlanItemId == input.TrainingPlanItemId.Value);
        if (input.TrainingCenterPlanItemId.HasValue)
            queryable = queryable.Where(x => x.TrainingCenterPlanItemId == input.TrainingCenterPlanItemId.Value);
        if (input.TenantCourseId.HasValue)
            queryable = queryable.Where(x => x.TenantCourseId == input.TenantCourseId.Value);
        if (input.CourseType.HasValue)
            queryable = queryable.Where(x => x.CourseType == input.CourseType.Value);
        if (input.Status.HasValue)
            queryable = queryable.Where(x => x.Status == input.Status.Value);
        if (input.PlanYear.HasValue)
            queryable = queryable.Where(x => x.PlanYear == input.PlanYear.Value);
        if (input.PreferredQuarter.HasValue)
            queryable = queryable.Where(x => x.PreferredQuarter == input.PreferredQuarter.Value);

        // Materialise the candidate set (after the DB-level filters) so we can compute
        // ExecutionStage per row. For tenants in the hundreds of sessions this fits
        // comfortably in memory; once that scales out, push stage to a persisted column.
        var entities = await AsyncExecuter.ToListAsync(queryable.OrderByDescending(x => x.CreationTime));

        if (entities.Count == 0)
        {
            return new PagedResultDto<CourseSessionDto>(0, new List<CourseSessionDto>());
        }

        var ctx = await LoadEnrichmentContextAsync(entities);

        var allDtos = entities.Select(s => BuildListDto(s, ctx)).ToList();

        if (input.ExecutionStage.HasValue)
            allDtos = allDtos.Where(d => d.ExecutionStage == input.ExecutionStage.Value).ToList();

        var totalCount = allDtos.Count;
        var paged = allDtos.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        return new PagedResultDto<CourseSessionDto>(totalCount, paged);
    }

    // ────────────────────────────────────────────────────────────────────
    // POST — SelectPriceQuote (atomic: quote.IsSelected + session dates + status)
    // ────────────────────────────────────────────────────────────────────
    [Authorize(TrainingPermissions.CourseSession.SelectQuote)]
    public async Task<CourseSessionDetailDto> SelectPriceQuoteAsync(Guid id, SelectSessionPriceQuoteDto input)
    {
        var session = await sessionRepo.GetAsync(id);

        if (session.Status != SessionStatus.Planned)
            throw new BusinessException("Training:Session:NotInPlannedStatus");

        if (input.ActualEndDate < input.ActualStartDate)
            throw new BusinessException("Training:Session:InvalidActualDates");

        await priceQuoteValidator.ValidateForSessionSelectionAsync(id, input.PriceQuoteId);

        // Flip IsSelected off on the previous winner, if any.
        if (session.SelectedPriceQuoteId.HasValue &&
            session.SelectedPriceQuoteId.Value != input.PriceQuoteId)
        {
            var oldQuote = await priceQuoteRepo.FindAsync(session.SelectedPriceQuoteId.Value);
            if (oldQuote != null && oldQuote.IsSelected)
            {
                oldQuote.IsSelected = false;
                await priceQuoteRepo.UpdateAsync(oldQuote);
            }
        }

        var newQuote = await priceQuoteRepo.GetAsync(input.PriceQuoteId);
        newQuote.IsSelected = true;
        await priceQuoteRepo.UpdateAsync(newQuote);

        // Three writes on the session at once: winner pointer, dates, status.
        session.SelectedPriceQuoteId = input.PriceQuoteId;
        session.ActualStartDate = input.ActualStartDate;
        session.ActualEndDate = input.ActualEndDate;
        session.Status = SessionStatus.Scheduled;

        await sessionRepo.UpdateAsync(session, autoSave: true);

        return await BuildDetailDtoAsync(session);
    }

    // ────────────────────────────────────────────────────────────────────
    // POST — MarkInProgress (Scheduled → InProgress)
    // ────────────────────────────────────────────────────────────────────
    [Authorize(TrainingPermissions.CourseSession.MarkInProgress)]
    public async Task<CourseSessionDetailDto> MarkInProgressAsync(Guid id)
    {
        var session = await sessionRepo.GetAsync(id);
        if (session.Status != SessionStatus.Scheduled)
            throw new BusinessException("Training:Session:InvalidStatusTransition")
                .WithData("CurrentStatus", session.Status.ToString())
                .WithData("AttemptedTransition", "MarkInProgress");

        session.Status = SessionStatus.InProgress;
        await sessionRepo.UpdateAsync(session, autoSave: true);
        return await BuildDetailDtoAsync(session);
    }

    // ────────────────────────────────────────────────────────────────────
    // POST — MarkCompleted (InProgress → Completed)
    // ────────────────────────────────────────────────────────────────────
    [Authorize(TrainingPermissions.CourseSession.MarkCompleted)]
    public async Task<CourseSessionDetailDto> MarkCompletedAsync(Guid id)
    {
        var session = await sessionRepo.GetAsync(id);
        if (session.Status != SessionStatus.InProgress)
            throw new BusinessException("Training:Session:InvalidStatusTransition")
                .WithData("CurrentStatus", session.Status.ToString())
                .WithData("AttemptedTransition", "MarkCompleted");

        session.Status = SessionStatus.Completed;
        await sessionRepo.UpdateAsync(session, autoSave: true);
        return await BuildDetailDtoAsync(session);
    }

    // ────────────────────────────────────────────────────────────────────
    // POST — Cancel (Planned | Scheduled → Cancelled). No cascading deletes.
    // ────────────────────────────────────────────────────────────────────
    [Authorize(TrainingPermissions.CourseSession.Cancel)]
    public async Task<CourseSessionDetailDto> CancelAsync(Guid id, CancelSessionDto input)
    {
        var session = await sessionRepo.GetAsync(id);
        if (session.Status != SessionStatus.Planned && session.Status != SessionStatus.Scheduled)
            throw new BusinessException("Training:Session:CannotCancelAfterInProgress");

        session.Status = SessionStatus.Cancelled;
        session.CancellationReason = input.Reason;
        session.CancelledAt = DateTime.UtcNow;
        session.CancelledById = CurrentUser.Id;

        await sessionRepo.UpdateAsync(session, autoSave: true);
        return await BuildDetailDtoAsync(session);
    }

    // ════════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Bag of batch-fetched lookups shared between rows of a list page so each session
    /// row doesn't trigger N+1 queries. All sub-collections are keyed on SessionId.
    /// </summary>
    private sealed record EnrichmentContext(
        Dictionary<Guid, TenantCourse> TenantCourses,
        Dictionary<Guid, CourseCatalog> Catalogs,
        Dictionary<Guid, TravelInstruction> TravelInstructionBySession,
        Dictionary<Guid, List<TravelAllowancePayment>> AllowancesBySession,
        Dictionary<Guid, CoursePayment> CoursePaymentBySession,
        Dictionary<Guid, int> NomineeCountBySession);

    private async Task<EnrichmentContext> LoadEnrichmentContextAsync(List<CourseSession> sessions)
    {
        var sessionIds = sessions.Select(s => s.Id).ToList();
        var tenantCourseIds = sessions.Select(s => s.TenantCourseId).Distinct().ToList();

        var tenantCourses = await AsyncExecuter.ToListAsync(
            (await tenantCourseRepo.GetQueryableAsync()).Where(tc => tenantCourseIds.Contains(tc.Id)));
        var catalogIds = tenantCourses.Select(tc => tc.CatalogCourseId).Distinct().ToList();
        var catalogs = await AsyncExecuter.ToListAsync(
            (await catalogRepo.GetQueryableAsync()).Where(c => catalogIds.Contains(c.Id)));

        var travels = await AsyncExecuter.ToListAsync(
            (await travelInstructionRepo.GetQueryableAsync())
                .Where(t => t.SessionId.HasValue && sessionIds.Contains(t.SessionId!.Value)));
        var allowances = await AsyncExecuter.ToListAsync(
            (await travelAllowanceRepo.GetQueryableAsync())
                .Where(p => p.SessionId.HasValue && sessionIds.Contains(p.SessionId!.Value)));
        var coursePayments = await AsyncExecuter.ToListAsync(
            (await coursePaymentRepo.GetQueryableAsync())
                .Where(p => p.SessionId.HasValue && sessionIds.Contains(p.SessionId!.Value)));

        var nomCounts = await AsyncExecuter.ToListAsync(
            (await sessionNominationRepo.GetQueryableAsync())
                .Where(n => sessionIds.Contains(n.SessionId))
                .GroupBy(n => n.SessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() }));

        return new EnrichmentContext(
            TenantCourses: tenantCourses.ToDictionary(tc => tc.Id),
            Catalogs: catalogs.ToDictionary(c => c.Id),
            TravelInstructionBySession: travels
                .Where(t => t.SessionId.HasValue)
                .GroupBy(t => t.SessionId!.Value)
                .ToDictionary(g => g.Key, g => g.First()),
            AllowancesBySession: allowances
                .Where(p => p.SessionId.HasValue)
                .GroupBy(p => p.SessionId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList()),
            CoursePaymentBySession: coursePayments
                .Where(p => p.SessionId.HasValue)
                .GroupBy(p => p.SessionId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.CreationTime).First()),
            NomineeCountBySession: nomCounts.ToDictionary(x => x.SessionId, x => x.Count));
    }

    private CourseSessionDto BuildListDto(CourseSession s, EnrichmentContext ctx)
    {
        var dto = new CourseSessionDto
        {
            Id = s.Id,
            TrainingPlanItemId = s.TrainingPlanItemId,
            TrainingCenterPlanItemId = s.TrainingCenterPlanItemId,
            TenantCourseId = s.TenantCourseId,
            CourseType = s.CourseType,
            PreferredQuarter = s.PreferredQuarter,
            PlanYear = s.PlanYear,
            ActualStartDate = s.ActualStartDate,
            ActualEndDate = s.ActualEndDate,
            SelectedPriceQuoteId = s.SelectedPriceQuoteId,
            Status = s.Status,
            NomineesCount = ctx.NomineeCountBySession.TryGetValue(s.Id, out var nc) ? nc : 0,
            CancellationReason = s.CancellationReason,
            CancelledAt = s.CancelledAt,
            CancelledById = s.CancelledById,
            CreationTime = s.CreationTime,
            CreatorId = s.CreatorId,
            LastModificationTime = s.LastModificationTime,
            LastModifierId = s.LastModifierId,
        };

        if (ctx.TenantCourses.TryGetValue(s.TenantCourseId, out var tc)
            && ctx.Catalogs.TryGetValue(tc.CatalogCourseId, out var cat))
        {
            dto.TenantCourseNameAr = cat.CourseNameAr;
            dto.TenantCourseNameEn = cat.CourseNameEn;
        }

        dto.ExecutionStage = ComputeStage(s,
            ctx.TravelInstructionBySession.GetValueOrDefault(s.Id),
            ctx.AllowancesBySession.GetValueOrDefault(s.Id) ?? new List<TravelAllowancePayment>(),
            ctx.CoursePaymentBySession.GetValueOrDefault(s.Id),
            dto.NomineesCount);

        return dto;
    }

    private async Task<CourseSessionDetailDto> BuildDetailDtoAsync(CourseSession s)
    {
        var ctx = await LoadEnrichmentContextAsync(new List<CourseSession> { s });

        var dto = new CourseSessionDetailDto
        {
            Id = s.Id,
            TrainingPlanItemId = s.TrainingPlanItemId,
            TrainingCenterPlanItemId = s.TrainingCenterPlanItemId,
            TenantCourseId = s.TenantCourseId,
            CourseType = s.CourseType,
            PreferredQuarter = s.PreferredQuarter,
            PlanYear = s.PlanYear,
            ActualStartDate = s.ActualStartDate,
            ActualEndDate = s.ActualEndDate,
            Status = s.Status,
            SelectedPriceQuoteId = s.SelectedPriceQuoteId,
            CancellationReason = s.CancellationReason,
            CancelledAt = s.CancelledAt,
            CancelledById = s.CancelledById,
            CreationTime = s.CreationTime,
            CreatorId = s.CreatorId,
            LastModificationTime = s.LastModificationTime,
            LastModifierId = s.LastModifierId,
        };

        if (ctx.TenantCourses.TryGetValue(s.TenantCourseId, out var tc)
            && ctx.Catalogs.TryGetValue(tc.CatalogCourseId, out var cat))
        {
            dto.TenantCourseNameAr = cat.CourseNameAr;
            dto.TenantCourseNameEn = cat.CourseNameEn;
        }

        var nomineeCount = ctx.NomineeCountBySession.TryGetValue(s.Id, out var nc) ? nc : 0;
        dto.NomineesCount = nomineeCount;
        dto.ExecutionStage = ComputeStage(s,
            ctx.TravelInstructionBySession.GetValueOrDefault(s.Id),
            ctx.AllowancesBySession.GetValueOrDefault(s.Id) ?? new List<TravelAllowancePayment>(),
            ctx.CoursePaymentBySession.GetValueOrDefault(s.Id),
            nomineeCount);

        // Info Bar: duration + approved cost + requesting unit sourced from the originating
        // TrainingPlanItem. TrainingCenterPlanItemId is a Phase 4C-β path; not handled here.
        if (s.TrainingPlanItemId.HasValue)
        {
            var planItem = await planItemRepo.FindAsync(s.TrainingPlanItemId.Value);
            if (planItem != null)
            {
                dto.DurationDays = planItem.DurationDays;
                dto.EstimatedDateFrom = planItem.EstimatedDateFrom;
                dto.EstimatedDateTo = planItem.EstimatedDateTo;
                if (planItem.UnitId.HasValue)
                {
                    var ou = await orgUnitRepository.FindAsync(planItem.UnitId.Value);
                    if (ou != null) dto.UnitName = ou.DisplayName;
                }
            }
            var cost = await planItemCostCalculator.GetEstimatedCostAsync(s.TrainingPlanItemId.Value);
            dto.ApprovedCostOMR = cost;
            dto.UnitTotalOMR = cost;
        }

        // Selected quote enrichment — providers fetched on demand.
        if (s.SelectedPriceQuoteId.HasValue)
        {
            var quote = await priceQuoteRepo.FindAsync(s.SelectedPriceQuoteId.Value);
            if (quote != null)
            {
                dto.SelectedPriceQuoteAmountOMR = quote.QuotedPriceOMR;
                dto.SelectedPriceQuoteProviderId = quote.ProviderId;
                var provider = await providerRepo.FindAsync(quote.ProviderId);
                if (provider != null) dto.SelectedPriceQuoteProviderNameAr = provider.ProviderNameAr;
            }
        }

        // Nominations — load + employee/rank enrichment in two batched roundtrips.
        var nominations = await AsyncExecuter.ToListAsync(
            (await sessionNominationRepo.GetQueryableAsync())
                .Where(n => n.SessionId == s.Id)
                .OrderBy(n => n.CreationTime));

        if (nominations.Count == 0)
        {
            return dto;
        }

        var employeeIds = nominations
            .SelectMany(n => new[] { n.EmployeeId, n.OriginalEmployeeId })
            .Distinct()
            .ToList();
        var rankIds = nominations.Select(n => n.RankId).Distinct().ToList();

        var employees = await AsyncExecuter.ToListAsync(
            (await employeeRepo.GetQueryableAsync()).Where(e => employeeIds.Contains(e.Id)));
        var ranks = await AsyncExecuter.ToListAsync(
            (await rankRepo.GetQueryableAsync()).Where(r => rankIds.Contains(r.Id)));

        var employeeMap = employees.ToDictionary(e => e.Id);
        var rankMap = ranks.ToDictionary(r => r.Id);

        dto.Nominations = nominations.Select(n =>
        {
            var row = new SessionNominationDto
            {
                Id = n.Id,
                SessionId = n.SessionId,
                EmployeeId = n.EmployeeId,
                OriginalEmployeeId = n.OriginalEmployeeId,
                WasSubstituted = n.WasSubstituted,
                SubstitutionReason = n.SubstitutionReason,
                RankId = n.RankId,
                CreationTime = n.CreationTime,
            };

            if (employeeMap.TryGetValue(n.EmployeeId, out var emp))
            {
                row.EmployeeNameAr = emp.FullNameAr;
                row.EmployeeNameEn = emp.FullNameEn;
            }
            if (employeeMap.TryGetValue(n.OriginalEmployeeId, out var orig))
                row.OriginalEmployeeNameAr = orig.FullNameAr;
            if (rankMap.TryGetValue(n.RankId, out var rank))
                row.RankNameAr = rank.NameAr;

            return row;
        }).ToList();

        // Info Bar: officer/enlisted breakdown via Rank.PersonnelType ("Officer"/"Enlisted").
        foreach (var n in nominations)
        {
            if (!rankMap.TryGetValue(n.RankId, out var rank)) continue;
            if (string.Equals(rank.PersonnelType, "Officer", StringComparison.OrdinalIgnoreCase))
                dto.OfficersCount++;
            else if (string.Equals(rank.PersonnelType, "Enlisted", StringComparison.OrdinalIgnoreCase))
                dto.EnlistedCount++;
        }

        return dto;
    }

    /// <summary>
    /// Maps (session × dependents) → SessionExecutionStage. Internal courses skip quotes,
    /// travel, and payments. Cancelled sessions are terminal.
    /// </summary>
    private static SessionExecutionStage ComputeStage(
        CourseSession s,
        TravelInstruction? ti,
        List<TravelAllowancePayment> allowances,
        CoursePayment? coursePayment,
        int expectedNomineeCount)
    {
        if (s.Status == SessionStatus.Cancelled)
            return SessionExecutionStage.NoExecutionPending;

        // Internal sessions have no quote, travel, allowance, or course-payment workflow.
        // They are ready for operational execution as soon as they are created Scheduled.
        if (s.CourseType == CourseType.Internal)
        {
            return s.Status switch
            {
                SessionStatus.Completed           => SessionExecutionStage.FinanciallyComplete,
                SessionStatus.FinanciallyClosed   => SessionExecutionStage.FinanciallyComplete,
                _ => SessionExecutionStage.AwaitingCompletion,
            };
        }

        if (s.Status == SessionStatus.Planned)
            return SessionExecutionStage.AwaitingQuoteSelection;

        // Patches 4 + 5 (v4.10.5) — travel instruction + per-nominee allowances apply only to
        // ExternalInternational. ExternalLocal skips these stages.
        var isInternational = s.CourseType == CourseType.ExternalInternational;

        if (isInternational && (ti == null || ti.Status != TravelInstructionStatus.Issued))
            return SessionExecutionStage.AwaitingTravelInstruction;

        if (isInternational && expectedNomineeCount > 0)
        {
            var confirmed = allowances.Count(a => a.Status == PaymentStatus.Confirmed);
            if (confirmed < expectedNomineeCount)
                return SessionExecutionStage.AwaitingTravelAllowances;
        }

        // Both external arms: course payment must be confirmed.
        if (coursePayment == null || coursePayment.Status != PaymentStatus.Confirmed)
            return SessionExecutionStage.AwaitingCoursePayment;

        return s.Status switch
        {
            SessionStatus.Completed           => SessionExecutionStage.FinanciallyComplete,
            SessionStatus.FinanciallyClosed   => SessionExecutionStage.FinanciallyComplete,
            _ => SessionExecutionStage.AwaitingCompletion,
        };
    }
}
