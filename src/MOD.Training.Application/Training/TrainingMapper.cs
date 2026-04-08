using MOD.Training.Training.Catalog;
using MOD.Training.Training.Catalog.Dtos;
using MOD.Training.Training.CourseFields.Dtos;
using MOD.Training.Training.CourseProposals.Dtos;
using MOD.Training.Training.TenantCourses;
using MOD.Training.Training.TenantCourses.Dtos;
using Riok.Mapperly.Abstractions;

namespace YourApp.Training;

/// <summary>
/// Mapperly source-generated mapper. Replaces AutoMapper profile for ABP 10.x.
/// All mappings are compile-time — no runtime reflection.
/// </summary>
[Mapper]
public static partial class TrainingMapper
{
    // ── Course Catalog ──
    public static partial CourseCatalogDto ToDto(this CourseCatalog source);
    public static partial CourseCatalog ToEntity(this CreateUpdateCourseCatalogDto source);
    public static partial void MapTo(this CreateUpdateCourseCatalogDto source, CourseCatalog target);

    // ── Course Fields ──
    public static partial CourseFieldDto ToDto(this CourseField source);
    public static partial CourseField ToEntity(this CreateUpdateCourseFieldDto source);
    public static partial void MapTo(this CreateUpdateCourseFieldDto source, CourseField target);

    // ── Catalog Enrollment Conditions ──
    public static partial CatalogEnrollmentConditionDto ToDto(this CatalogEnrollmentCondition source);

    // ── Course Proposals ──
    public static partial CourseProposalDto ToDto(this CourseProposal source);

    // ── Tenant Courses ──
    public static partial TenantCourseDto ToDto(this TenantCourse source);
    public static partial void MapTo(this UpdateTenantCourseDto source, TenantCourse target);

    // ── Tenant Course Conditions ──
    public static partial TenantCourseConditionDto ToDto(this TenantCourseCondition source);
}
