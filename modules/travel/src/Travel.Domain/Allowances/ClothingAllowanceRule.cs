using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Travel.Allowances;

public class ClothingAllowanceRule : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public decimal FullAmount { get; private set; }
    public decimal AnnualPartialAmount { get; private set; }
    public int FullPaymentPeriodYears { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }
    public ICollection<ClothingAllowanceRuleRank> Ranks { get; private set; } = new List<ClothingAllowanceRuleRank>();

    protected ClothingAllowanceRule()
    {
    }

    public ClothingAllowanceRule(
        Guid id,
        string name,
        decimal fullAmount,
        decimal annualPartialAmount,
        int fullPaymentPeriodYears,
        int priority,
        bool isActive,
        IEnumerable<Guid> rankIds) : base(id)
    {
        Update(name, fullAmount, annualPartialAmount, fullPaymentPeriodYears, priority, isActive);
        SetRanks(rankIds);
    }

    public void Update(
        string name,
        decimal fullAmount,
        decimal annualPartialAmount,
        int fullPaymentPeriodYears,
        int priority,
        bool isActive)
    {
        if (fullAmount < 0 || annualPartialAmount < 0)
        {
            throw new BusinessException(TravelErrorCodes.NegativeAmount);
        }

        if (fullPaymentPeriodYears < 1)
        {
            throw new BusinessException(TravelErrorCodes.InvalidAllowanceSegment);
        }

        Name = name;
        FullAmount = fullAmount;
        AnnualPartialAmount = annualPartialAmount;
        FullPaymentPeriodYears = fullPaymentPeriodYears;
        Priority = priority;
        IsActive = isActive;
    }

    public void SetRanks(IEnumerable<Guid> rankIds)
    {
        Ranks.Clear();

        foreach (var rankId in rankIds.Distinct())
        {
            Ranks.Add(new ClothingAllowanceRuleRank(Guid.NewGuid(), Id, rankId));
        }
    }
}
