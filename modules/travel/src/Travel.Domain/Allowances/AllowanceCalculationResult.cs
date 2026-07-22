using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.Domain.Values;

namespace Travel.Allowances;

public class AllowanceCalculationResult : ValueObject
{
    public decimal DailyBaseRate { get; init; }
    public decimal? AccommodationMultiplier { get; init; }
    public int TotalDays { get; init; }
    public decimal OverseasTotal { get; init; }
    public List<AllowanceCalculationSegment> OverseasSegments { get; init; } = new();
    public List<AllowanceEmployeeCalculation> EmployeeCalculations { get; init; } = new();
    public decimal ClothingAmount { get; init; }
    public bool ClothingEligible { get; init; }
    public string ClothingCalculationNote { get; init; } = string.Empty;
    public decimal DeductionAmount { get; init; }
    public decimal NetTotal => OverseasTotal + ClothingAmount - DeductionAmount;
    public DateTime CalculatedAt { get; init; }

    protected AllowanceCalculationResult()
    {
    }

    public AllowanceCalculationResult(
        decimal dailyBaseRate,
        decimal? accommodationMultiplier,
        int totalDays,
        decimal overseasTotal,
        List<AllowanceCalculationSegment> overseasSegments,
        List<AllowanceEmployeeCalculation> employeeCalculations,
        decimal clothingAmount,
        bool clothingEligible,
        string clothingCalculationNote,
        decimal deductionAmount,
        DateTime calculatedAt)
    {
        DailyBaseRate = dailyBaseRate;
        AccommodationMultiplier = accommodationMultiplier;
        TotalDays = totalDays;
        OverseasTotal = overseasTotal;
        OverseasSegments = overseasSegments;
        EmployeeCalculations = employeeCalculations;
        ClothingAmount = clothingAmount;
        ClothingEligible = clothingEligible;
        ClothingCalculationNote = clothingCalculationNote;
        DeductionAmount = deductionAmount;
        CalculatedAt = calculatedAt;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return DailyBaseRate;
        yield return AccommodationMultiplier ?? 1m;
        yield return TotalDays;
        yield return OverseasTotal;
        yield return OverseasSegments.Sum(s => s.Amount);
        yield return ClothingAmount;
        yield return ClothingEligible;
        yield return DeductionAmount;
        yield return NetTotal;
        yield return CalculatedAt;
    }
}
