using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities;

namespace MOD.Training.Training.TenantCourses;

/// <summary>
/// Enrollment conditions for a tenant course.
/// Auto-copied from CatalogEnrollmentConditions when TenantCourse is created.
/// Read-only / locked — tenant cannot add, edit, or delete these.
/// </summary>
public class TenantCourseCondition : Entity<Guid>
{
    public TenantCourseCondition(Guid id) : base(id)
    {
        
    }
    public TenantCourseCondition()
    {
        
    }
    public Guid TenantCourseId { get; set; }
    public ConditionType ConditionType { get; set; }
    public string ConditionValue { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    // Navigation
    public TenantCourse? TenantCourse { get; set; }
}
