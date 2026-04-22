using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Plans.Dtos;

public class PlanItemFinancialItemRankDto : FullAuditedEntityDto<Guid>
{
    public Guid PlanItemFinancialItemId { get; set; }
    public Guid RankId { get; set; }
    public string RankNameAr { get; set; } = string.Empty;
    public string RankNameEn { get; set; } = string.Empty;
    public int NomineeCount { get; set; }
    public decimal RatePerUnitOMR { get; set; }
    public decimal SubtotalOMR { get; set; }
}

public class UpdateRateDto
{
    public decimal NewRate { get; set; }
}
