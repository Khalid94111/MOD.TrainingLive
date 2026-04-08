using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;
using MOD.Training.Training.Catalog;
using MOD.Training.Training.Catalog.Dtos;
using MOD.Training.Training.TenantCourses;
using MOD.Training.Training.TenantCourses.Dtos;
using MOD.Training.Training.CourseFields.Dtos;
using MOD.Training.Training.CourseProposals.Dtos;

namespace MOD.Training.Training;

// ── Course Catalog ──

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CourseCatalogToCourseCatalogDtoMapper : MapperBase<CourseCatalog, CourseCatalogDto>
{
    public override partial CourseCatalogDto Map(CourseCatalog source);
    public override partial void Map(CourseCatalog source, CourseCatalogDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CreateUpdateCourseCatalogDtoToCourseCatalogMapper : MapperBase<CreateUpdateCourseCatalogDto, CourseCatalog>
{
    public override partial CourseCatalog Map(CreateUpdateCourseCatalogDto source);
    public override partial void Map(CreateUpdateCourseCatalogDto source, CourseCatalog destination);
}

// ── Course Fields ──

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CourseFieldToCourseFieldDtoMapper : MapperBase<CourseField, CourseFieldDto>
{
    public override partial CourseFieldDto Map(CourseField source);
    public override partial void Map(CourseField source, CourseFieldDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CreateUpdateCourseFieldDtoToCourseFieldMapper : MapperBase<CreateUpdateCourseFieldDto, CourseField>
{
    public override partial CourseField Map(CreateUpdateCourseFieldDto source);
    public override partial void Map(CreateUpdateCourseFieldDto source, CourseField destination);
}

// ── Catalog Enrollment Conditions ──

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CatalogEnrollmentConditionToDtoMapper : MapperBase<CatalogEnrollmentCondition, CatalogEnrollmentConditionDto>
{
    public override partial CatalogEnrollmentConditionDto Map(CatalogEnrollmentCondition source);
    public override partial void Map(CatalogEnrollmentCondition source, CatalogEnrollmentConditionDto destination);
}

// ── Course Proposals ──

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CourseProposalToCourseProposalDtoMapper : MapperBase<CourseProposal, CourseProposalDto>
{
    public override partial CourseProposalDto Map(CourseProposal source);
    public override partial void Map(CourseProposal source, CourseProposalDto destination);
}

// ── Tenant Courses ──

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TenantCourseToTenantCourseDtoMapper : MapperBase<TenantCourse, TenantCourseDto>
{
    public override partial TenantCourseDto Map(TenantCourse source);
    public override partial void Map(TenantCourse source, TenantCourseDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class UpdateTenantCourseDtoToTenantCourseMapper : MapperBase<UpdateTenantCourseDto, TenantCourse>
{
    public override partial TenantCourse Map(UpdateTenantCourseDto source);
    public override partial void Map(UpdateTenantCourseDto source, TenantCourse destination);
}

// ── Tenant Course Conditions ──

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TenantCourseConditionToDtoMapper : MapperBase<TenantCourseCondition, TenantCourseConditionDto>
{
    public override partial TenantCourseConditionDto Map(TenantCourseCondition source);
    public override partial void Map(TenantCourseCondition source, TenantCourseConditionDto destination);
}