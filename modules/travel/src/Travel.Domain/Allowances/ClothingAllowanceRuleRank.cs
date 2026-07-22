using System;
using Volo.Abp.Domain.Entities;

namespace Travel.Allowances;

public class ClothingAllowanceRuleRank : Entity<Guid>
{
    public Guid ClothingAllowanceRuleId { get; private set; }
    public Guid RankId { get; private set; }

    protected ClothingAllowanceRuleRank()
    {
    }

    public ClothingAllowanceRuleRank(Guid id, Guid clothingAllowanceRuleId, Guid rankId) : base(id)
    {
        ClothingAllowanceRuleId = clothingAllowanceRuleId;
        RankId = rankId;
    }
}
