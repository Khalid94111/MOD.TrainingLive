using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Values;

namespace Travel.Allowances;

public class AllowanceSnapshot : ValueObject
{
    public decimal OverseasTotal { get; init; }
    public decimal ClothingTotal { get; init; }
    public decimal DeductionAmount { get; init; }
    public decimal NetTotal => OverseasTotal + ClothingTotal - DeductionAmount;
    public DateTime CalculatedAt { get; init; }

    protected AllowanceSnapshot()
    {
    }

    public AllowanceSnapshot(decimal overseasTotal, decimal clothingTotal, decimal deductionAmount, DateTime calculatedAt)
    {
        OverseasTotal = overseasTotal;
        ClothingTotal = clothingTotal;
        DeductionAmount = deductionAmount;
        CalculatedAt = calculatedAt;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return OverseasTotal;
        yield return ClothingTotal;
        yield return DeductionAmount;
        yield return CalculatedAt;
    }
}
