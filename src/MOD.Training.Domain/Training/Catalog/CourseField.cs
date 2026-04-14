using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace MOD.Training.Training.Catalog;

/// <summary>
/// Course field/domain lookup (e.g., IT, Engineering, Military Sciences).
/// Supra-tenant — managed by System Admin.
/// </summary>
public class CourseField : FullAuditedEntity<Guid>
{
    public CourseField(Guid id) : base(id)
    {
        
    }
    public CourseField()
    {
        
    }
    public string FieldNameAr { get; set; } = null!;
    public string FieldNameEn { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
