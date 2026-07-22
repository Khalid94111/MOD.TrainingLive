using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Travel.Settings;
using Travel.TravelRequests;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Settings;

namespace Travel.Allowances;

public class AllowanceCalculatorService : DomainService
{
    private readonly ISettingProvider _settingProvider;
    private readonly IRepository<AllowanceRule, Guid> _ruleRepository;
    private readonly IRepository<AllowanceRuleSegment, Guid> _ruleSegmentRepository;
    private readonly IRepository<AllowanceRate, Guid> _rateRepository;
    private readonly IRepository<ClothingAllowanceRule, Guid> _clothingRuleRepository;
    private readonly IRepository<ClothingAllowanceRuleRank, Guid> _clothingRuleRankRepository;
    private readonly IRepository<AccommodationRule, Guid> _accommodationRuleRepository;
    private readonly IRepository<AccommodationRuleAllowanceRule, Guid> _accommodationRuleLinkRepository;
    private readonly IRepository<EmployeeClothingHistory, Guid> _clothingHistoryRepository;
    private readonly IRankLookup _rankLookup;

    public AllowanceCalculatorService(
        ISettingProvider settingProvider,
        IRepository<AllowanceRule, Guid> ruleRepository,
        IRepository<AllowanceRuleSegment, Guid> ruleSegmentRepository,
        IRepository<AllowanceRate, Guid> rateRepository,
        IRepository<ClothingAllowanceRule, Guid> clothingRuleRepository,
        IRepository<ClothingAllowanceRuleRank, Guid> clothingRuleRankRepository,
        IRepository<AccommodationRule, Guid> accommodationRuleRepository,
        IRepository<AccommodationRuleAllowanceRule, Guid> accommodationRuleLinkRepository,
        IRepository<EmployeeClothingHistory, Guid> clothingHistoryRepository,
        IRankLookup rankLookup)
    {
        _settingProvider = settingProvider;
        _ruleRepository = ruleRepository;
        _ruleSegmentRepository = ruleSegmentRepository;
        _rateRepository = rateRepository;
        _clothingRuleRepository = clothingRuleRepository;
        _clothingRuleRankRepository = clothingRuleRankRepository;
        _accommodationRuleRepository = accommodationRuleRepository;
        _accommodationRuleLinkRepository = accommodationRuleLinkRepository;
        _clothingHistoryRepository = clothingHistoryRepository;
        _rankLookup = rankLookup;
    }

    public virtual async Task<AllowanceCalculationResult> CalculateAsync(
        TravelRequest request,
        List<TravelRequestEmployee> employees,
        bool includeClothingAllowance = false)
    {
        var maxDays = await _settingProvider.GetAsync<int>(TravelManagementSettings.OverseasMaxDays);
        var days = Math.Min(request.GetDurationInDays(), maxDays);

        if (employees.Count == 0)
        {
            employees =
            [
                new TravelRequestEmployee(
                    Guid.NewGuid(),
                    request.Id,
                    Guid.Empty,
                    string.Empty,
                    string.Empty,
                    null,
                    string.Empty,
                    request.Category,
                    request.DailyAllowanceRate)
            ];
        }

        var baseRatesByEmployeeId = new Dictionary<Guid, decimal>();
        foreach (var employee in employees)
        {
            baseRatesByEmployeeId[employee.EmployeeId] = await GetBaseRateAsync(request, employee);
        }

        var highestBaseRate = request.UseHighestAllowance && baseRatesByEmployeeId.Count > 0
            ? baseRatesByEmployeeId.Values.Max()
            : (decimal?)null;

        var overseasTotal = 0m;
        var baseRates = new List<decimal>();
        var segmentTotals = new Dictionary<(int FromDay, int ToDay, decimal Percentage), decimal>();
        var employeeCalculations = new List<AllowanceEmployeeCalculation>();
        decimal? accommodationMultiplier = null;
        var hasRealEmployees = employees.Any(e => e.EmployeeId != Guid.Empty);

        foreach (var employee in employees)
        {
            var (rule, ruleSegments) = await FindMatchingRuleAsync(request, employee, days);
            var baseRate = highestBaseRate ?? baseRatesByEmployeeId[employee.EmployeeId];
            baseRates.Add(baseRate);

            var (employeeTotalBeforeAccommodation, employeeSegments) = CalculateOverseasWithSegments(days, baseRate, ruleSegments);
            var accommodationPaymentPercentage = await GetAccommodationPaymentPercentageAsync(request, rule?.Id);
            accommodationMultiplier ??= accommodationPaymentPercentage < 100m
                ? accommodationPaymentPercentage / 100m
                : null;
            employeeSegments = ApplyAccommodationRule(employeeSegments, accommodationPaymentPercentage);
            var employeeTotal = accommodationPaymentPercentage == 100m
                ? employeeTotalBeforeAccommodation
                : employeeSegments.Sum(segment => segment.Amount);

            foreach (var segment in employeeSegments)
            {
                var key = (segment.FromDay, segment.ToDay, segment.Percentage);
                segmentTotals[key] = segmentTotals.GetValueOrDefault(key) + segment.Amount;
                overseasTotal += segment.Amount;
            }

            if (hasRealEmployees)
            {
                employeeCalculations.Add(new AllowanceEmployeeCalculation
                {
                    EmployeeId = employee.EmployeeId,
                    EmployeeName = employee.EmployeeName,
                    EmployeeNumber = employee.EmployeeNumber,
                    RankName = employee.RankName,
                    Category = request.Category,
                    DailyRate = baseRate,
                    AccommodationPaymentPercentage = accommodationPaymentPercentage,
                    OverseasTotal = employeeTotal,
                    Segments = employeeSegments,
                    HasMatchingRule = rule != null
                });
            }
        }

        var segments = segmentTotals
            .OrderBy(x => x.Key.FromDay)
            .Select(x => new AllowanceCalculationSegment(
                x.Key.FromDay,
                x.Key.ToDay,
                x.Key.ToDay - x.Key.FromDay + 1,
                x.Key.Percentage,
                x.Value))
            .ToList();

        // Calculate clothing allowance
        decimal clothingAmount = 0m;
        var clothingNote = string.Empty;
        var clothingEligible = false;

        if (includeClothingAllowance && employees.Count > 0)
        {
            var clothingResult = await CalculateClothingForEmployeesAsync(request, employees);
            clothingAmount = clothingResult.Amount;
            clothingNote = clothingResult.Note;
            clothingEligible = clothingResult.Eligible;

            foreach (var employeeCalculation in employeeCalculations)
            {
                if (!clothingResult.EmployeeResults.TryGetValue(employeeCalculation.EmployeeId, out var employeeClothing))
                {
                    continue;
                }

                employeeCalculation.ClothingAmount = employeeClothing.Amount;
                employeeCalculation.ClothingEligible = employeeClothing.Eligible;
                employeeCalculation.ClothingIsFullPayment = employeeClothing.IsFullPayment;
                employeeCalculation.ClothingAllowanceRuleId = employeeClothing.RuleId;
                employeeCalculation.ClothingCalculationNote = employeeClothing.Note;
            }
        }

        return new AllowanceCalculationResult(
            dailyBaseRate: baseRates.Distinct().Count() == 1 ? baseRates[0] : baseRates.Sum(),
            accommodationMultiplier: accommodationMultiplier,
            totalDays: days,
            overseasTotal: overseasTotal,
            overseasSegments: segments,
            employeeCalculations: employeeCalculations,
            clothingAmount: clothingAmount,
            clothingEligible: clothingEligible,
            clothingCalculationNote: clothingNote,
            deductionAmount: 0m,
            calculatedAt: Clock.Now);
    }

    private async Task<(AllowanceRule? Rule, List<AllowanceRuleSegment> Segments)> FindMatchingRuleAsync(TravelRequest request, TravelRequestEmployee employee, int totalDays)
    {
        var rules = await _ruleRepository.GetListAsync(r =>
            r.IsActive &&
            r.AllowanceType == AllowanceType.Daily &&
            (r.TravelTypeDefinitionId == null || r.TravelTypeDefinitionId == request.TravelTypeDefinitionId) &&
            (r.Category == null || r.Category == request.Category));

        if (!rules.Any())
            return (null, []);

        var rule = rules
            .OrderByDescending(r => r.Priority)
            .FirstOrDefault();

        if (rule == null)
            return (null, []);

        var segments = await _ruleSegmentRepository.GetListAsync(s => s.AllowanceRuleId == rule.Id);
        return (rule, segments
            .Where(s => s.AppliesToTotalDays(totalDays))
            .OrderBy(s => s.FromDay)
            .ToList());
    }

    private async Task<decimal> GetAccommodationPaymentPercentageAsync(
        TravelRequest request,
        Guid? allowanceRuleId)
    {
        if (!request.IncludesAccommodation || !allowanceRuleId.HasValue)
        {
            return 100m;
        }

        var links = await _accommodationRuleLinkRepository.GetListAsync(x => x.AllowanceRuleId == allowanceRuleId.Value);
        var linkedAccommodationRuleIds = links.Select(x => x.AccommodationRuleId).Distinct().ToList();

        if (linkedAccommodationRuleIds.Count == 0)
        {
            return 100m;
        }

        var rules = await _accommodationRuleRepository.GetListAsync(r =>
            r.IsActive &&
            linkedAccommodationRuleIds.Contains(r.Id));

        return rules
            .OrderByDescending(r => r.Priority)
            .FirstOrDefault()
            ?.PaymentPercentage ?? 100m;
    }

    private static List<AllowanceCalculationSegment> ApplyAccommodationRule(
        List<AllowanceCalculationSegment> segments,
        decimal paymentPercentage)
    {
        if (paymentPercentage == 100m)
        {
            return segments;
        }

        var multiplier = paymentPercentage / 100m;
        return segments.Select(s => new AllowanceCalculationSegment(
            s.FromDay,
            s.ToDay,
            s.Days,
            s.Percentage * multiplier,
            s.Amount * multiplier)).ToList();
    }

    private async Task<decimal> GetBaseRateAsync(TravelRequest request, TravelRequestEmployee employee)
    {
        if (!employee.RankId.HasValue)
            return employee.DailyAllowanceRate > 0 ? employee.DailyAllowanceRate : request.DailyAllowanceRate;

        var rates = await _rateRepository.GetListAsync(r =>
            r.IsActive &&
            r.AllowanceType == AllowanceType.Daily &&
            r.RankId == employee.RankId.Value &&
            r.Category == request.Category);

        return rates.FirstOrDefault()?.Amount ?? (employee.DailyAllowanceRate > 0 ? employee.DailyAllowanceRate : request.DailyAllowanceRate);
    }

    private (decimal Total, List<AllowanceCalculationSegment> Segments) CalculateOverseasWithSegments(
        int days, decimal dailyRate, List<AllowanceRuleSegment> ruleSegments)
    {
        var segments = new List<AllowanceCalculationSegment>();
        var total = 0m;

        if (!ruleSegments.Any())
        {
            // Fallback: 100% for all days if no rule found
            var amount = days * dailyRate;
            segments.Add(new AllowanceCalculationSegment(1, days, days, 100m, amount));
            return (amount, segments);
        }

        var orderedSegments = ruleSegments.OrderBy(s => s.FromDay).ToList();

        foreach (var seg in orderedSegments)
        {
            var overlapFrom = Math.Max(seg.FromDay, 1);
            var overlapTo = Math.Min(seg.ToDay, days);

            if (overlapFrom > overlapTo)
                continue;

            var segmentDays = overlapTo - overlapFrom + 1;
            var percentage = seg.Percentage / 100m;
            var segmentAmount = segmentDays * dailyRate * percentage;

            segments.Add(new AllowanceCalculationSegment(
                overlapFrom,
                overlapTo,
                segmentDays,
                seg.Percentage,
                segmentAmount));

            total += segmentAmount;
        }

        // Handle days beyond last segment (100% fallback)
        var lastSegmentEnd = orderedSegments.Max(s => s.ToDay);
        if (days > lastSegmentEnd)
        {
            var extraDays = days - lastSegmentEnd;
            var extraAmount = extraDays * dailyRate;
            segments.Add(new AllowanceCalculationSegment(
                lastSegmentEnd + 1,
                days,
                extraDays,
                100m,
                extraAmount));
            total += extraAmount;
        }

        return (total, segments);
    }

    private async Task<(decimal Amount, string Note, bool Eligible, Dictionary<Guid, (decimal Amount, string Note, bool Eligible, bool IsFullPayment, Guid? RuleId)> EmployeeResults)> CalculateClothingForEmployeesAsync(
        TravelRequest request,
        List<TravelRequestEmployee> employees)
    {
        var totalAmount = 0m;
        var notes = new List<string>();
        var anyEligible = false;
        var employeeResults = new Dictionary<Guid, (decimal Amount, string Note, bool Eligible, bool IsFullPayment, Guid? RuleId)>();

        foreach (var employee in employees)
        {
            if (!employee.RankId.HasValue)
            {
                var missingRankNote = $"{employee.EmployeeName}: NoRankAssigned";
                notes.Add(missingRankNote);
                employeeResults[employee.EmployeeId] = (0m, missingRankNote, false, false, null);
                continue;
            }

            var history = await _clothingHistoryRepository.GetListAsync(h =>
                h.EmployeeId == employee.EmployeeId &&
                h.TravelRequestId != request.Id,
                cancellationToken: default);

            var rule = await GetClothingRuleAsync(employee.RankId.Value);

            if (rule == null)
            {
                var missingRuleNote = $"{employee.EmployeeName}: NoClothingRuleConfigured";
                notes.Add(missingRuleNote);
                employeeResults[employee.EmployeeId] = (0m, missingRuleNote, false, false, null);
                continue;
            }

            decimal amount;
            string note;
            bool isFullPayment;
            var fullAmount = rule.FullAmount;
            var annualPartialAmount = rule.AnnualPartialAmount;
            var fullPaymentPeriodYears = rule.FullPaymentPeriodYears;

            if (history.Count == 0)
            {
                amount = fullAmount;
                note = $"{employee.EmployeeName}: FirstTimeFullPayment:{fullAmount}";
                anyEligible = true;
                isFullPayment = true;
            }
            else
            {
                var lastPayment = history
                    .OrderByDescending(h => h.PaidAt)
                    .First();
                var anchorDate = lastPayment.PaidAt;
                var elapsedYears = (int)((Clock.Now.Date - anchorDate.Date).TotalDays / 365.25);

                if (elapsedYears >= fullPaymentPeriodYears)
                {
                    amount = fullAmount;
                    note = $"{employee.EmployeeName}: FullPayment:{fullPaymentPeriodYears}:{fullAmount}";
                    anyEligible = true;
                    isFullPayment = true;
                }
                else
                {
                    var partialEntitlement = elapsedYears * annualPartialAmount;
                    amount = partialEntitlement;
                    isFullPayment = false;
                    note = elapsedYears > 0
                        ? $"{employee.EmployeeName}: PartialPayment:{elapsedYears}:{amount}"
                        : $"{employee.EmployeeName}: NotEligible:{fullPaymentPeriodYears - elapsedYears}";
                    if (amount > 0) anyEligible = true;
                }
            }

            totalAmount += amount;
            notes.Add(note);
            employeeResults[employee.EmployeeId] = (amount, note, amount > 0, isFullPayment, rule.Id);
        }

        return (totalAmount, string.Join("; ", notes), anyEligible, employeeResults);
    }

    private async Task<ClothingAllowanceRule?> GetClothingRuleAsync(Guid rankId)
    {
        var links = await _clothingRuleRankRepository.GetListAsync(x => x.RankId == rankId);
        var ruleIds = links.Select(x => x.ClothingAllowanceRuleId).Distinct().ToList();

        if (ruleIds.Count == 0)
        {
            return null;
        }

        var rules = await _clothingRuleRepository.GetListAsync(x => x.IsActive && ruleIds.Contains(x.Id));
        return rules
            .OrderByDescending(x => x.Priority)
            .FirstOrDefault();
    }

}
