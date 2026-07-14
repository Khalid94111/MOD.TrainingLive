using MOD.Training.Training.Catalog;
using MOD.Training.Training.Catalog.Dtos;
using MOD.Training.Training.CourseFields.Dtos;
using MOD.Training.Training.CourseProposals.Dtos;
using MOD.Training.Training.TenantCourses;
using MOD.Training.Training.TenantCourses.Dtos;
using Riok.Mapperly.Abstractions;

namespace MOD.Training.Training;

/// <summary>
/// Mapperly source-generated mapper. Replaces AutoMapper profile for ABP 10.x.
/// All mappings are compile-time — no runtime reflection.
/// </summary>
[Mapper]
public static partial class TrainingMapper
{
    // ── Course Catalog ──
    // NOTE: Migrated to AutoMapper (TrainingAutoMapperProfile)
    // public static partial CourseCatalogDto ToDto(this CourseCatalog source);
    // public static partial CourseCatalog ToEntity(this CreateUpdateCourseCatalogDto source);
    // public static partial void MapTo(this CreateUpdateCourseCatalogDto source, CourseCatalog target);

    // ── Course Fields ──
    public static partial CourseFieldDto ToDto(this CourseField source);
    public static partial CourseField ToEntity(this CreateUpdateCourseFieldDto source);
    public static partial void MapTo(this CreateUpdateCourseFieldDto source, CourseField target);

    // ── Course Proposals ──
    // NOTE: Migrated to AutoMapper (TrainingAutoMapperProfile)
    // public static partial CourseProposalDto ToDto(this CourseProposal source);

    // ── Tenant Courses ──
    // NOTE: Migrated to AutoMapper (TrainingAutoMapperProfile)
    // public static partial TenantCourseDto ToDto(this TenantCourse source);
    // public static partial void MapTo(this UpdateTenantCourseDto source, TenantCourse target);
}
