using System;
using System.ComponentModel.DataAnnotations;
using Travel.TravelTypes;

namespace Travel.Allowances;

public class CreateUpdateAllowanceRateDto
{
    [Required]
    public Guid RankId { get; set; }

    [Required]
    public AllowanceCategory Category { get; set; } = AllowanceCategory.A;

    [Required]
    public AllowanceType AllowanceType { get; set; }

    [Range(0, 999999)]
    public decimal Amount { get; set; }

    [Range(0, 999999)]
    public decimal? AnnualPartialAmount { get; set; }

    public TicketClass TicketClass { get; set; } = TicketClass.Economy;

    public bool IsActive { get; set; } = true;
}
