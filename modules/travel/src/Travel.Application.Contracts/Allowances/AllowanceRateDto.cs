using System;
using Travel.TravelTypes;
using Volo.Abp.Application.Dtos;

namespace Travel.Allowances;

public class AllowanceRateDto : FullAuditedEntityDto<Guid>
{
    public Guid RankId { get; set; }
    public string RankName { get; set; } = string.Empty;
    public AllowanceCategory Category { get; set; }
    public AllowanceType AllowanceType { get; set; }
    public decimal Amount { get; set; }
    public decimal? AnnualPartialAmount { get; set; }
    public TicketClass TicketClass { get; set; } = TicketClass.Economy;
    public bool IsActive { get; set; }
}
