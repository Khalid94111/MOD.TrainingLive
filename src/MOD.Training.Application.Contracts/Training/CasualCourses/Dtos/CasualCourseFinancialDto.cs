using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class CasualCourseFinancialDto : FullAuditedEntityDto<Guid>
{
    public Guid? TenantId { get; set; }
    public Guid CasualCourseId { get; set; }
    public Guid FinancialItemId { get; set; }

    public string FinancialItemName { get; set; } = "";
    public bool IsPerDay { get; set; }
    public bool IsPerNominee { get; set; }
    public int ExtraDaysBefore { get; set; }
    public int ExtraDaysAfter { get; set; }
    public int EffectiveDays { get; set; }

    public decimal EstimatedAmountOMR { get; set; }
    public decimal? ActualAmountOMR { get; set; }

    public FinancialAmountSource Source { get; set; }
    public string? Notes { get; set; }

    public List<CasualCourseFinancialItemRankDto> Ranks { get; set; } = new();
}
