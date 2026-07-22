using System;
using Volo.Abp.Domain.Entities;

namespace Travel.Allowances;

public class AccommodationRuleAllowanceRule : Entity<Guid>
{
    public Guid AccommodationRuleId { get; private set; }
    public Guid AllowanceRuleId { get; private set; }

    protected AccommodationRuleAllowanceRule()
    {
    }

    public AccommodationRuleAllowanceRule(Guid id, Guid accommodationRuleId, Guid allowanceRuleId) : base(id)
    {
        AccommodationRuleId = accommodationRuleId;
        AllowanceRuleId = allowanceRuleId;
    }
}
