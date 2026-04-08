using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace MOD.Training.Training.Catalog;

/// <summary>
/// Supra-tenant course catalog. System Admin manages.
/// Primary purpose: prevent duplicate course names across all tenants.
/// </summary>
public class CourseCatalog : FullAuditedAggregateRoot<Guid>
{
    public string CourseNameAr { get; set; } = null!;
    public string CourseNameEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }

    /// <summary>Military / Civilian</summary>
    public string Category { get; set; } = null!;

    /// <summary>Mandatory / Qualifying</summary>
    public string Nature { get; set; } = null!;

    public Guid FieldId { get; set; }

    public ResultType ResultType { get; set; }

    public bool RequiresEvaluation { get; set; }
    public bool RequiresProviderEvaluation { get; set; }
    public bool HasCertificate { get; set; }

    /// <summary>
    /// If true, employee must complete course evaluation before certificate becomes visible.
    /// Only relevant when RequiresEvaluation = true.
    /// </summary>
    public bool EvaluationBlocksCertificate { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public CourseField? Field { get; set; }
}
