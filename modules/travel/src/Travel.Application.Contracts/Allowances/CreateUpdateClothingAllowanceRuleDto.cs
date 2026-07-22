using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Travel.Allowances;

public class CreateUpdateClothingAllowanceRuleDto
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 999999)]
    public decimal FullAmount { get; set; }

    [Range(0, 999999)]
    public decimal AnnualPartialAmount { get; set; }

    [Range(1, 50)]
    public int FullPaymentPeriodYears { get; set; } = 3;

    public int Priority { get; set; }

    public bool IsActive { get; set; } = true;

    public List<Guid> RankIds { get; set; } = new();
}
