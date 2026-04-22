using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Plans.Dtos;

public class PlanItemFinancialItemDto : FullAuditedEntityDto<Guid>
{
    public Guid PlanItemId { get; set; }
    public Guid FinancialItemId { get; set; }
    public string FinancialItemName { get; set; } = string.Empty; // Resolved
    public decimal EstimatedAmountOMR { get; set; }
    public decimal? EstimatedAmountUSD { get; set; }
    public decimal? ActualAmountOMR { get; set; }
    public decimal? ActualAmountUSD { get; set; }
    public string? Notes { get; set; }
}
public class UpdateAmountDto
{
    public decimal EstimatedAmountOMR { get; set; }
}

public class UpdateNotesDto
{
    public string? Notes { get; set; }
}