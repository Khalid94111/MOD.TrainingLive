using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Travel.Allowances;

public class CreateUpdateAccommodationRuleDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 100)]
    public decimal PaymentPercentage { get; set; } = 100m;

    [Range(0, 9999)]
    public int Priority { get; set; }

    public bool IsActive { get; set; } = true;

    public List<Guid> AllowanceRuleIds { get; set; } = new();
}
