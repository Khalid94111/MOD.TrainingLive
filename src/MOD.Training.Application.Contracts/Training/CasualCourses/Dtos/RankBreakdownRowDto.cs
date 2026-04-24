using System;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class RankBreakdownRowDto
{
    public Guid RankId { get; set; }
    public string RankNameAr { get; set; } = "";
    public int NomineeCount { get; set; }
    public decimal RatePerUnitOMR { get; set; }
    public string RateSource { get; set; } = "DefaultAmount";
    public decimal SubtotalOMR { get; set; }
}
