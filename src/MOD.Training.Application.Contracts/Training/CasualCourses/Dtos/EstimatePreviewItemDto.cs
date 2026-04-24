using System.Collections.Generic;
using System;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class EstimatePreviewItemDto
{
    public Guid FinancialItemId { get; set; }
    public string FinancialItemName { get; set; } = "";
    public bool IsPerDay { get; set; }
    public bool IsPerNominee { get; set; }
    public int EffectiveDays { get; set; }
    public decimal TotalAmountOMR { get; set; }
    public List<RankBreakdownRowDto>? RankBreakdown { get; set; }
}
