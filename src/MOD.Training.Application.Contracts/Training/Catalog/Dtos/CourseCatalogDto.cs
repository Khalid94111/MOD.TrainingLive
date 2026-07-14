using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Catalog.Dtos;

public class CourseCatalogDto : FullAuditedEntityDto<Guid>
{
    public string CourseNameAr { get; set; } = null!;
    public string CourseNameEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string Category { get; set; } = null!;
    public string Nature { get; set; } = null!;
    public Guid FieldId { get; set; }
    public string? FieldNameAr { get; set; }
    public string? FieldNameEn { get; set; }
    public ResultType ResultType { get; set; }
    public bool RequiresEvaluation { get; set; }
    public bool RequiresProviderEvaluation { get; set; }
    public bool HasCertificate { get; set; }
    public bool EvaluationBlocksCertificate { get; set; }
    public bool IsActive { get; set; }
}

public class CreateUpdateCourseCatalogDto
{
    public string CourseNameAr { get; set; } = null!;
    public string CourseNameEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string Category { get; set; } = null!;
    public string Nature { get; set; } = null!;
    public Guid FieldId { get; set; }
    public ResultType ResultType { get; set; }
    public bool RequiresEvaluation { get; set; }
    public bool RequiresProviderEvaluation { get; set; }
    public bool HasCertificate { get; set; }
    public bool EvaluationBlocksCertificate { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Used for catalog search/filter parameters beyond standard paging.
/// </summary>
public class CourseCatalogGetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public Guid? FieldId { get; set; }
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
}
