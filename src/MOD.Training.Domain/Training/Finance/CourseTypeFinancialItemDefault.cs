using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Finance;

public class CourseTypeFinancialItemDefault : Entity<Guid>, IMultiTenant
{
    public CourseTypeFinancialItemDefault(Guid id) : base(id)
    {
        
    }
    public CourseTypeFinancialItemDefault()
    {
        
    }
    public Guid? TenantId { get; set; }
    public CourseType CourseType { get; set; }
    public Guid FinancialItemId { get; set; }
    public int SortOrder { get; set; }

    // Navigation
    public FinancialItem FinancialItem { get; set; } = default!;
}
