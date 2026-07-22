using System;
using Volo.Abp.Domain.Entities;

namespace Travel.Allowances;

public class AllowanceRuleSegment : Entity<Guid>
{
    public Guid AllowanceRuleId { get; private set; }
    public int FromDay { get; private set; }
    public int ToDay { get; private set; }
    public decimal Percentage { get; private set; }
    public int? AppliesWhenTotalDaysFrom { get; private set; }
    public int? AppliesWhenTotalDaysTo { get; private set; }

    protected AllowanceRuleSegment()
    {
    }

    public AllowanceRuleSegment(
        Guid id,
        Guid allowanceRuleId,
        int fromDay,
        int toDay,
        decimal percentage,
        int? appliesWhenTotalDaysFrom,
        int? appliesWhenTotalDaysTo)
    {
        Id = id;
        AllowanceRuleId = allowanceRuleId;
        FromDay = fromDay;
        ToDay = toDay;
        Percentage = percentage;
        AppliesWhenTotalDaysFrom = appliesWhenTotalDaysFrom;
        AppliesWhenTotalDaysTo = appliesWhenTotalDaysTo;
    }

    public bool AppliesToTotalDays(int totalDays)
    {
        return (!AppliesWhenTotalDaysFrom.HasValue || AppliesWhenTotalDaysFrom.Value <= totalDays) &&
               (!AppliesWhenTotalDaysTo.HasValue || AppliesWhenTotalDaysTo.Value >= totalDays);
    }
}
