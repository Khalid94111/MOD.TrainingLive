using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.CasualCourses;

public class CasualCourseFinancialItemRank : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid CasualCourseFinancialId { get; set; }
    public Guid RankId { get; set; }
    public int NomineeCount { get; set; }
    public decimal RatePerUnitOMR { get; set; }
    public decimal SubtotalOMR { get; set; }
    public string RateSource { get; set; } = "DefaultAmount";

    protected CasualCourseFinancialItemRank() { }

    public CasualCourseFinancialItemRank(
        Guid id,
        Guid casualCourseFinancialId,
        Guid rankId,
        int nomineeCount,
        decimal ratePerUnit,
        decimal subtotal,
        string rateSource) : base(id)
    {
        CasualCourseFinancialId = casualCourseFinancialId;
        RankId = rankId;
        NomineeCount = nomineeCount;
        RatePerUnitOMR = ratePerUnit;
        SubtotalOMR = subtotal;
        RateSource = rateSource;
    }
}
