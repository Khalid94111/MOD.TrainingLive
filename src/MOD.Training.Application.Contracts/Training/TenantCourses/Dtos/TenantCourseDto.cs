using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.TenantCourses.Dtos;

public class TenantCourseDto : FullAuditedEntityDto<Guid>
{
    public Guid CatalogCourseId { get; set; }
    public string CatalogCourseNameAr { get; set; } = null!;
    public string CatalogCourseNameEn { get; set; } = null!;
    public string? CatalogCourseFieldNameAr { get; set; }
    public string? CatalogCourseCategory { get; set; }
    public int? DefaultCapacity { get; set; }
    public int? DefaultDurationWeeks { get; set; }
    public ResultType ResultType { get; set; }
    public bool RequiresEvaluation { get; set; }
    public bool RequiresProviderEvaluation { get; set; }
    public bool HasCertificate { get; set; }
    public bool EvaluationBlocksCertificate { get; set; }
    public bool IsActive { get; set; }
    public int ConditionsCount { get; set; }
    public string? AddedByName { get; set; }
    public DateTime AddedAt { get; set; }
}

/// <summary>
/// Used when adding course(s) from catalog to tenant.
/// Supports multi-select — catalogCourseIds array.
/// </summary>
public class AddFromCatalogDto
{
    public Guid[] CatalogCourseIds { get; set; } = Array.Empty<Guid>();
}

public class UpdateTenantCourseDto
{
    public int? DefaultCapacity { get; set; }
    public int? DefaultDurationWeeks { get; set; }
    public ResultType ResultType { get; set; }
    public bool RequiresEvaluation { get; set; }
    public bool RequiresProviderEvaluation { get; set; }
    public bool HasCertificate { get; set; }
    public bool EvaluationBlocksCertificate { get; set; }
    public bool IsActive { get; set; }
}

public class TenantCourseGetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public Guid? FieldId { get; set; }
    public ResultType? ResultType { get; set; }
    public bool? IsActive { get; set; }
}
