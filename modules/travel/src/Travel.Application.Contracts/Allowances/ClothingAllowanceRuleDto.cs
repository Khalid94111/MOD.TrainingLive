using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Travel.Allowances;

public class ClothingAllowanceRuleDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal FullAmount { get; set; }
    public decimal AnnualPartialAmount { get; set; }
    public int FullPaymentPeriodYears { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public List<Guid> RankIds { get; set; } = new();
    public List<string> RankNames { get; set; } = new();
}
