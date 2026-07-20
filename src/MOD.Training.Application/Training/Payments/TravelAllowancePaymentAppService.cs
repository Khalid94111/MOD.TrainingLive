using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Execution;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Hr;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Nominations;
using MOD.Training.Training.Payments.Dtos;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Payments;

/// <summary>
/// Per-nominee travel cost CRUD + Confirm. Supports both polymorphic arms (casual / session).
///
/// Server-controlled fields (never trusted from client):
///   • TotalOMR — recomputed on every save as Σ of the five components.
///   • PersonnelType — derived from nominee's Rank.PersonnelType string ("Officer"/"Enlisted").
///   • Status — Draft → Confirmed (terminal). External* fields are reserved for Nebras sync.
///
/// Default-value enrichment on Create (suggestions; Finance can override before Confirm):
///   • CASUAL arm — pulls per-nominee defaults from <c>CasualCourseFinancialItemRank.SubtotalOMR / NomineeCount</c>
///     for items whose linked <c>FinancialItem.ItemType</c> matches Allowance / Insurance / Visa / Clothing.
///   • SESSION arm — defaults all five components to 0 in 4B-β (no rank breakdown wiring on the session arm yet).
///   • Both arms zero out Insurance / VisaFees if the parent's TravelInstruction toggles say so.
/// </summary>
[Authorize(TrainingPaymentsPermissions.TravelAllowance.Default)]
public class TravelAllowancePaymentAppService(
    IRepository<TravelAllowancePayment, Guid> repository,
    IRepository<CasualCourse, Guid> casualCourseRepo,
    IRepository<CasualCourseNomination, Guid> casualNominationRepo,
    IRepository<CasualCourseFinancialItem, Guid> casualItemRepo,
    IRepository<CourseSession, Guid> sessionRepo,
    IRepository<Nomination, Guid> nominationRepo,
    IRepository<Course, Guid> courseRepo,
    IRepository<FinancialItem, Guid> financialItemRepo,
    IRepository<TravelInstruction, Guid> travelInstructionRepo,
    IRepository<Employee, Guid> employeeRepo,
    IRepository<Rank, Guid> rankRepo,
    TravelDayCalculator travelDayCalculator,
    CourseNameResolver courseNameResolver,
    TravelAllowancePaymentToDtoMapper toDtoMapper,
    CreateUpdateTravelAllowancePaymentToEntityMapper toEntityMapper)
    : ApplicationService, ITravelAllowancePaymentAppService
{
    public async Task<TravelAllowancePaymentDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        return await BuildDtoAsync(entity);
    }

    public async Task<PagedResultDto<TravelAllowancePaymentDto>> GetListAsync(
        TravelAllowancePaymentGetListInput input)
    {
        var queryable = await repository.GetQueryableAsync();

        if (input.CasualCourseId.HasValue)
            queryable = queryable.Where(x => x.CasualCourseId == input.CasualCourseId.Value);
        if (input.SessionId.HasValue)
            queryable = queryable.Where(x => x.SessionId == input.SessionId.Value);
        if (input.NominationId.HasValue)
            queryable = queryable.Where(x => x.NominationId == input.NominationId.Value);
        if (input.Status.HasValue)
            queryable = queryable.Where(x => x.Status == input.Status.Value);
        if (input.PersonnelType.HasValue)
            queryable = queryable.Where(x => x.PersonnelType == input.PersonnelType.Value);

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        queryable = queryable.OrderByDescending(x => x.CreationTime).PageBy(input);
        var entities = await AsyncExecuter.ToListAsync(queryable);

        var dtos = await BuildDtosAsync(entities);

        // Optional employee-name search applied after enrichment.
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var needle = input.Search.Trim();
            dtos = dtos.Where(d => d.EmployeeNameAr?.Contains(needle) == true).ToList();
            totalCount = dtos.Count;
        }

        return new PagedResultDto<TravelAllowancePaymentDto>(totalCount, dtos);
    }

    [Authorize(TrainingPaymentsPermissions.TravelAllowance.Create)]
    public async Task<TravelAllowancePaymentDto> CreateAsync(CreateUpdateTravelAllowancePaymentDto input)
    {
        ValidatePolymorphicParent(input);
        ValidateAmounts(input);

        await ValidateParentReadyAsync(input.CasualCourseId, input.SessionId);
        var travel = await GetIssuedTravelInstructionAsync(input.CasualCourseId, input.SessionId);
        var (employeeId, personnelType, rankId) = await ResolveNomineeAsync(input);
        await ValidateNominationInCourseAsync(input);
        await ValidateNotDuplicateAsync(input.NominationId, ignoreId: null);

        var entity = toEntityMapper.Map(input);
        entity.PersonnelType = personnelType;

        await ApplyServerDefaultsAsync(entity, input, travel, rankId);
        entity.TotalOMR = ComputeTotal(entity);
        entity.Status = PaymentStatus.Draft;

        await repository.InsertAsync(entity, autoSave: true);
        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPaymentsPermissions.TravelAllowance.Update)]
    public async Task<TravelAllowancePaymentDto> UpdateAsync(
        Guid id, CreateUpdateTravelAllowancePaymentDto input)
    {
        var entity = await repository.GetAsync(id);
        EnsureDraft(entity, on: "Update");

        ValidateAmounts(input);

        // Polymorphic parent + nomination set on Create, immutable on Update — preserve originals.
        var sessionOriginal = entity.SessionId;
        var casualOriginal = entity.CasualCourseId;
        var nominationOriginal = entity.NominationId;
        var personnelOriginal = entity.PersonnelType;
        var statusOriginal = entity.Status;

        toEntityMapper.Map(input, entity);

        entity.SessionId = sessionOriginal;
        entity.CasualCourseId = casualOriginal;
        entity.NominationId = nominationOriginal;
        entity.PersonnelType = personnelOriginal;
        entity.Status = statusOriginal;
        entity.TotalOMR = ComputeTotal(entity);

        await repository.UpdateAsync(entity, autoSave: true);
        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPaymentsPermissions.TravelAllowance.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        EnsureDraft(entity, on: "Delete");
        await repository.DeleteAsync(id);
    }

    /// <summary>
    /// Returns the preview defaults that Create would apply, without persisting.
    /// Mirrors <see cref="ApplyServerDefaultsAsync"/> but skips the entity build + insert,
    /// and exposes the underlying per-day rate so the UI can render a "days × rate = total"
    /// breakdown directly.
    /// </summary>
    [Authorize(TrainingPaymentsPermissions.TravelAllowance.Create)]
    public async Task<TravelAllowancePaymentDefaultsDto> GetDefaultsAsync(
        Guid casualCourseId, Guid nominationId)
    {
        var stubInput = new CreateUpdateTravelAllowancePaymentDto
        {
            CasualCourseId = casualCourseId,
            SessionId = null,
            NominationId = nominationId,
        };
        await ValidateParentReadyAsync(casualCourseId, sessionId: null);
        var travel = await GetIssuedTravelInstructionAsync(casualCourseId, sessionId: null);
        var (_, personnelType, rankId) = await ResolveNomineeAsync(stubInput);
        await ValidateNominationInCourseAsync(stubInput);

        var breakdown = await ResolveCasualCourseBreakdownAsync(casualCourseId, rankId);
        var effectiveDays = travelDayCalculator.GetEffectiveTravelDays(travel);

        var dailyRate = breakdown.AllowanceDailyRate;
        // Travel-allowance preview = days × rate (frontend's chosen UX). The actual
        // stored total on Save Draft equals the per-person Allowance subtotal — these
        // typically agree because subtotal = rate × effectiveDays × nomineeCount and
        // perPerson = subtotal / nomineeCount = rate × effectiveDays.
        var travelAllowance = Math.Round(dailyRate * effectiveDays, 3, MidpointRounding.AwayFromZero);

        return new TravelAllowancePaymentDefaultsDto
        {
            DailyAllowanceRateOMR = dailyRate,
            TravelAllowanceOMR = travelAllowance,
            TicketAmountOMR = breakdown.PerPerson.GetValueOrDefault(FinancialItemType.Ticket),
            ClothingAllowanceOMR = breakdown.PerPerson.GetValueOrDefault(FinancialItemType.Clothing),
            InsuranceOMR = travel.InsuranceArranged ? breakdown.PerPerson.GetValueOrDefault(FinancialItemType.Insurance) : 0m,
            VisaFeesOMR = travel.VisaRequired ? breakdown.PerPerson.GetValueOrDefault(FinancialItemType.Visa) : 0m,
            EffectiveTravelDays = effectiveDays,
            PersonnelType = personnelType,
        };
    }

    [Authorize(TrainingPaymentsPermissions.TravelAllowance.Confirm)]
    public async Task<TravelAllowancePaymentDto> ConfirmAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        EnsureDraft(entity, on: "Confirm");

        // Re-check that the TravelInstruction is still Issued at confirmation time.
        await GetIssuedTravelInstructionAsync(entity.CasualCourseId, entity.SessionId);

        entity.Status = PaymentStatus.Confirmed;
        entity.ConfirmedAt = Clock.Now;
        entity.ConfirmedById = CurrentUser.Id;

        await repository.UpdateAsync(entity, autoSave: true);
        return await BuildDtoAsync(entity);
    }

    // ── Validation helpers ──────────────────────────────────────────────

    private static void ValidatePolymorphicParent(CreateUpdateTravelAllowancePaymentDto input)
    {
        if (input.CasualCourseId.HasValue == input.SessionId.HasValue)
            throw new BusinessException("Training:TravelAllowancePayment:OnePolymorphicParentRequired");
    }

    private static void ValidateAmounts(CreateUpdateTravelAllowancePaymentDto input)
    {
        if (input.TicketAmountOMR < 0 || input.TravelAllowanceOMR < 0
            || input.ClothingAllowanceOMR < 0 || input.InsuranceOMR < 0
            || input.VisaFeesOMR < 0)
        {
            throw new BusinessException("Training:TravelAllowancePayment:NegativeAmount");
        }
    }

    private async Task ValidateParentReadyAsync(Guid? casualCourseId, Guid? sessionId)
    {
        if (casualCourseId.HasValue)
        {
            var course = await casualCourseRepo.FindAsync(casualCourseId.Value)
                ?? throw new EntityNotFoundException(typeof(CasualCourse), casualCourseId.Value);
            if (course.Status != CasualCourseStatus.THApproved)
                throw new BusinessException("Training:TravelAllowancePayment:CourseNotApproved");
        }
        else
        {
            var session = await sessionRepo.FindAsync(sessionId!.Value)
                ?? throw new EntityNotFoundException(typeof(CourseSession), sessionId.Value);
            if (session.CourseType != CourseType.ExternalInternational)
                throw new BusinessException("Training:TravelAllowancePayment:SessionTravelNotRequired");
            if (session.Status == SessionStatus.Cancelled)
                throw new BusinessException("Training:TravelAllowancePayment:CourseNotApproved");
        }
    }

    private async Task<TravelInstruction> GetIssuedTravelInstructionAsync(Guid? casualCourseId, Guid? sessionId)
    {
        var queryable = await travelInstructionRepo.GetQueryableAsync();
        var ti = casualCourseId.HasValue
            ? await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.CasualCourseId == casualCourseId.Value))
            : await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.SessionId == sessionId!.Value));

        if (ti == null || ti.Status != TravelInstructionStatus.Issued)
            throw new BusinessException("Training:TravelAllowancePayment:TravelInstructionNotIssued");

        return ti;
    }

    private async Task<(Guid EmployeeId, PersonnelType PersonnelType, Guid RankId)> ResolveNomineeAsync(
        CreateUpdateTravelAllowancePaymentDto input)
    {
        Guid employeeId;
        if (input.CasualCourseId.HasValue)
        {
            var nomination = await casualNominationRepo.FindAsync(input.NominationId)
                ?? throw new BusinessException("Training:TravelAllowancePayment:NominationNotInCourse");
            employeeId = nomination.EmployeeId;
        }
        else
        {
            var nomination = await nominationRepo.FindAsync(input.NominationId)
                ?? throw new BusinessException("Training:TravelAllowancePayment:NominationNotInCourse");
            employeeId = nomination.EmployeeId;
        }

        var employee = await employeeRepo.FindAsync(employeeId)
            ?? throw new BusinessException("Training:TravelAllowancePayment:NominationNotInCourse");
        var rank = await rankRepo.FindAsync(employee.RankId)
            ?? throw new BusinessException("Training:TravelAllowancePayment:NominationNotInCourse");

        var personnelType = string.Equals(rank.PersonnelType, "Officer", StringComparison.OrdinalIgnoreCase)
            ? PersonnelType.Officer
            : PersonnelType.Enlisted;
        return (employeeId, personnelType, rank.Id);
    }

    private async Task ValidateNominationInCourseAsync(CreateUpdateTravelAllowancePaymentDto input)
    {
        if (input.CasualCourseId.HasValue)
        {
            var queryable = await casualNominationRepo.GetQueryableAsync();
            var belongs = await AsyncExecuter.AnyAsync(queryable
                .Where(x => x.Id == input.NominationId && x.CasualCourseId == input.CasualCourseId.Value));
            if (!belongs)
                throw new BusinessException("Training:TravelAllowancePayment:NominationNotInCourse");
        }
        else
        {
            var queryable = await nominationRepo.GetQueryableAsync();
            var belongs = await AsyncExecuter.AnyAsync(queryable
                .Where(x => x.Id == input.NominationId && x.SessionId == input.SessionId!.Value));
            if (!belongs)
                throw new BusinessException("Training:TravelAllowancePayment:NominationNotInCourse");
        }
    }

    private async Task ValidateNotDuplicateAsync(Guid nominationId, Guid? ignoreId)
    {
        var queryable = await repository.GetQueryableAsync();
        var conflict = await AsyncExecuter.AnyAsync(
            queryable.Where(x => x.NominationId == nominationId && (ignoreId == null || x.Id != ignoreId)));
        if (conflict)
            throw new BusinessException("Training:TravelAllowancePayment:DuplicateForNomination");
    }

    private static void EnsureDraft(TravelAllowancePayment entity, string on)
    {
        if (entity.Status != PaymentStatus.Draft)
            throw new BusinessException("Training:TravelAllowancePayment:InvalidStatusTransition");
        _ = on; // reserved for telemetry; kept on signature for clarity at call sites
    }

    // ── Server-side computation ─────────────────────────────────────────

    private static decimal ComputeTotal(TravelAllowancePayment e)
        => e.TicketAmountOMR + e.TravelAllowanceOMR + e.ClothingAllowanceOMR + e.InsuranceOMR + e.VisaFeesOMR;

    /// <summary>
    /// Pre-populates suggested defaults on Create. Casual arm reads the rank breakdown
    /// already produced by the request workflow (per-nominee subtotal); session arm leaves
    /// zeros until annual-plan rank-breakdown wiring is added in a later patch.
    /// Both arms respect the TravelInstruction toggles for Insurance / Visa.
    /// </summary>
    private async Task ApplyServerDefaultsAsync(
        TravelAllowancePayment entity,
        CreateUpdateTravelAllowancePaymentDto input,
        TravelInstruction travel,
        Guid rankId)
    {
        // Honour TravelInstruction toggles — if not flagged, force these components to zero
        // regardless of what Finance entered. Spec: "InsuranceOMR derived from TravelInstruction
        // if InsuranceArranged = true; VisaFeesOMR derived if VisaRequired = true."
        if (!travel.InsuranceArranged) entity.InsuranceOMR = 0m;
        if (!travel.VisaRequired)      entity.VisaFeesOMR = 0m;

        // If client passed all-zero amounts (typical at Create time), suggest defaults from
        // the casual course's rank breakdown so Finance has a starting point.
        var clientAllZero = input.TicketAmountOMR == 0 && input.TravelAllowanceOMR == 0
            && input.ClothingAllowanceOMR == 0 && input.InsuranceOMR == 0 && input.VisaFeesOMR == 0;

        if (!clientAllZero) return;
        if (!entity.CasualCourseId.HasValue) return;  // session arm: defaults left at 0 (no wiring)

        var breakdown = await ResolveCasualCourseBreakdownAsync(entity.CasualCourseId.Value, rankId);
        entity.TravelAllowanceOMR = breakdown.PerPerson.GetValueOrDefault(FinancialItemType.Allowance);
        entity.ClothingAllowanceOMR = breakdown.PerPerson.GetValueOrDefault(FinancialItemType.Clothing);
        entity.TicketAmountOMR = breakdown.PerPerson.GetValueOrDefault(FinancialItemType.Ticket);
        if (travel.InsuranceArranged)
            entity.InsuranceOMR = breakdown.PerPerson.GetValueOrDefault(FinancialItemType.Insurance);
        if (travel.VisaRequired)
            entity.VisaFeesOMR = breakdown.PerPerson.GetValueOrDefault(FinancialItemType.Visa);
    }

    /// <summary>
    /// Computed snapshot of a casual course's per-rank financial breakdown for a single nominee.
    /// <see cref="PerPerson"/> exposes the per-nominee subtotal for each tracked FinancialItemType,
    /// while <see cref="AllowanceDailyRate"/> surfaces the Allowance item's per-day rate (RatePerUnitOMR)
    /// for the preview UI's "days × rate" breakdown.
    /// </summary>
    private sealed record CasualCourseBreakdown(
        IReadOnlyDictionary<FinancialItemType, decimal> PerPerson,
        decimal AllowanceDailyRate);

    private async Task<CasualCourseBreakdown> ResolveCasualCourseBreakdownAsync(
        Guid casualCourseId, Guid rankId)
    {
        var itemsQ = (await casualItemRepo.WithDetailsAsync(i => i.Ranks!))
            .Where(i => i.CasualCourseId == casualCourseId);
        var items = await AsyncExecuter.ToListAsync(itemsQ);
        if (items.Count == 0)
            return new CasualCourseBreakdown(new Dictionary<FinancialItemType, decimal>(), 0m);

        var financialIds = items.Select(i => i.FinancialItemId).Distinct().ToList();
        var fiQ = await financialItemRepo.GetQueryableAsync();
        var fis = await AsyncExecuter.ToListAsync(fiQ.Where(f => financialIds.Contains(f.Id)));
        var fiTypeById = fis.Where(f => f.ItemType.HasValue)
            .ToDictionary(f => f.Id, f => f.ItemType!.Value);

        var perPerson = new Dictionary<FinancialItemType, decimal>();
        var allowanceDailyRate = 0m;
        foreach (var item in items)
        {
            if (!fiTypeById.TryGetValue(item.FinancialItemId, out var type)) continue;
            if (type == FinancialItemType.CourseCost) continue;        // course payment, not travel
            if (type == FinancialItemType.Other) continue;             // unclassified, skip

            var rankRow = item.Ranks?.FirstOrDefault(r => r.RankId == rankId);
            if (rankRow == null || rankRow.NomineeCount == 0) continue;

            // Last-write-wins if duplicates exist (shouldn't happen — unique per item per course).
            perPerson[type] = rankRow.SubtotalOMR / rankRow.NomineeCount;
            if (type == FinancialItemType.Allowance)
                allowanceDailyRate = rankRow.RatePerUnitOMR;
        }
        return new CasualCourseBreakdown(perPerson, allowanceDailyRate);
    }

    // ── DTO enrichment ─────────────────────────────────────────────────

    private async Task<TravelAllowancePaymentDto> BuildDtoAsync(TravelAllowancePayment entity)
        => (await BuildDtosAsync(new[] { entity }))[0];

    private async Task<List<TravelAllowancePaymentDto>> BuildDtosAsync(IReadOnlyList<TravelAllowancePayment> entities)
    {
        var dtos = entities.Select(e => toDtoMapper.Map(e)).ToList();
        if (dtos.Count == 0) return dtos;

        // Resolve nominee → employee → rank.
        var casualNomIds = entities.Where(e => e.CasualCourseId.HasValue).Select(e => e.NominationId).Distinct().ToList();
        var sessionNomIds = entities.Where(e => e.SessionId.HasValue).Select(e => e.NominationId).Distinct().ToList();

        var casualNomLookup = await BatchByIdsAsync(casualNominationRepo, casualNomIds);
        var sessionNomLookup = await BatchByIdsAsync(nominationRepo, sessionNomIds);

        var employeeIds = casualNomLookup.Values.Select(n => n.EmployeeId)
            .Concat(sessionNomLookup.Values.Select(n => n.EmployeeId))
            .Distinct().ToList();
        var employeeLookup = await BatchByIdsAsync(employeeRepo, employeeIds);
        var rankIds = employeeLookup.Values.Select(e => e.RankId).Distinct().ToList();
        var rankLookup = await BatchByIdsAsync(rankRepo, rankIds);

        // Resolve travel instructions (one per parent course).
        var casualCourseIds = entities.Where(e => e.CasualCourseId.HasValue).Select(e => e.CasualCourseId!.Value).Distinct().ToList();
        var sessionIds = entities.Where(e => e.SessionId.HasValue).Select(e => e.SessionId!.Value).Distinct().ToList();
        var tiQ = await travelInstructionRepo.GetQueryableAsync();
        var travels = await AsyncExecuter.ToListAsync(
            tiQ.Where(x => (x.CasualCourseId.HasValue && casualCourseIds.Contains(x.CasualCourseId!.Value))
                        || (x.SessionId.HasValue && sessionIds.Contains(x.SessionId!.Value))));
        var travelByCasual = travels.Where(t => t.CasualCourseId.HasValue).ToDictionary(t => t.CasualCourseId!.Value);
        var travelBySession = travels.Where(t => t.SessionId.HasValue).ToDictionary(t => t.SessionId!.Value);

        // Resolve course names via TenantCourse → CourseCatalog (CourseNameResolver handles batching).
        // Phase 4C-α (v4.10.0): CourseSession exposes TenantCourseId directly — no Course indirection.
        var casualLookup = await BatchByIdsAsync(casualCourseRepo, casualCourseIds);
        var sessionLookup = await BatchByIdsAsync(sessionRepo, sessionIds);

        var tenantCourseIds = casualLookup.Values.Select(c => c.TenantCourseId)
            .Concat(sessionLookup.Values.Select(s => s.TenantCourseId))
            .Distinct().ToList();
        var nameLookup = await courseNameResolver.BatchResolveAsync(tenantCourseIds);

        for (var i = 0; i < dtos.Count; i++)
        {
            var dto = dtos[i];
            var ent = entities[i];

            Employee? employee = null;
            if (ent.CasualCourseId.HasValue && casualNomLookup.TryGetValue(ent.NominationId, out var cnom))
                employeeLookup.TryGetValue(cnom.EmployeeId, out employee);
            else if (ent.SessionId.HasValue && sessionNomLookup.TryGetValue(ent.NominationId, out var snom))
                employeeLookup.TryGetValue(snom.EmployeeId, out employee);

            if (employee != null)
            {
                dto.EmployeeNameAr = employee.FullNameAr;
                if (rankLookup.TryGetValue(employee.RankId, out var rank))
                    dto.RankNameAr = rank.NameAr;
            }

            Guid? tenantCourseId = null;
            if (ent.CasualCourseId.HasValue
                && casualLookup.TryGetValue(ent.CasualCourseId.Value, out var casual))
            {
                tenantCourseId = casual.TenantCourseId;
            }
            // Phase 4C-α (v4.10.0): CourseSession exposes TenantCourseId directly —
            // the Phase-3 indirection through Courses.CourseId is gone.
            else if (ent.SessionId.HasValue
                && sessionLookup.TryGetValue(ent.SessionId.Value, out var session))
            {
                tenantCourseId = session.TenantCourseId;
            }
            if (tenantCourseId.HasValue && nameLookup.TryGetValue(tenantCourseId.Value, out var name))
                dto.CourseNameAr = name.NameAr;

            TravelInstruction? travel = null;
            if (ent.CasualCourseId.HasValue) travelByCasual.TryGetValue(ent.CasualCourseId.Value, out travel);
            else if (ent.SessionId.HasValue) travelBySession.TryGetValue(ent.SessionId.Value, out travel);
            if (travel != null) dto.EffectiveTravelDays = travelDayCalculator.GetEffectiveTravelDays(travel);
        }

        return dtos;
    }

    private async Task<Dictionary<Guid, T>> BatchByIdsAsync<T>(IRepository<T, Guid> repo, IEnumerable<Guid> ids)
        where T : class, IEntity<Guid>
    {
        var idList = ids.Where(g => g != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0) return new Dictionary<Guid, T>();
        var queryable = await repo.GetQueryableAsync();
        var rows = await AsyncExecuter.ToListAsync(queryable.Where(x => idList.Contains(x.Id)));
        return rows.ToDictionary(x => x.Id);
    }
}
