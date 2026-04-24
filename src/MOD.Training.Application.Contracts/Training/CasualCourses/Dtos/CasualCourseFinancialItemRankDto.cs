using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class CasualCourseFinancialItemRankDto : EntityDto<Guid>
{
    public Guid CasualCourseFinancialId { get; set; }
    public Guid RankId { get; set; }
    public string RankNameAr { get; set; } = "";
    public int NomineeCount { get; set; }
    public decimal RatePerUnitOMR { get; set; }
    public decimal SubtotalOMR { get; set; }
    public string RateSource { get; set; } = "DefaultAmount";
}
