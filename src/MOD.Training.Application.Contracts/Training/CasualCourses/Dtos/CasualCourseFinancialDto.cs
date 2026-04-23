using MOD.Training.Training.Enums;
using System;
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

    public decimal EstimatedAmountOMR { get; set; }
    public decimal? ActualAmountOMR { get; set; }

    public FinancialAmountSource Source { get; set; }
    public string? Notes { get; set; }
}
