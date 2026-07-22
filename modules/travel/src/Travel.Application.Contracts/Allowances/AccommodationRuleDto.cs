using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Travel.Allowances;

public class AccommodationRuleDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal PaymentPercentage { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public List<Guid> AllowanceRuleIds { get; set; } = new();
    public List<string> AllowanceRuleNames { get; set; } = new();
}
