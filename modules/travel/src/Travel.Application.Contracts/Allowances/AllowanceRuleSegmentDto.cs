using System;
using Volo.Abp.Application.Dtos;

namespace Travel.Allowances;

public class AllowanceRuleSegmentDto : EntityDto<Guid>
{
    public Guid AllowanceRuleId { get; set; }
    public int FromDay { get; set; }
    public int ToDay { get; set; }
    public decimal Percentage { get; set; }
    public int? AppliesWhenTotalDaysFrom { get; set; }
    public int? AppliesWhenTotalDaysTo { get; set; }
}
