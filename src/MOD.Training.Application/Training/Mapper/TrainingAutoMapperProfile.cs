using AutoMapper;
using MOD.Training.Training.Catalog;
using MOD.Training.Training.Catalog.Dtos;
using MOD.Training.Training.CourseProposals.Dtos;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Finance.Dtos;
using MOD.Training.Training.TenantCourses;
using MOD.Training.Training.TenantCourses.Dtos;

namespace MOD.Training.Training;

public class TrainingAutoMapperProfile : Profile
{
    public TrainingAutoMapperProfile()
    {
        // CourseCatalog
        CreateMap<CourseCatalog, CourseCatalogDto>()
            .ForMember(dest => dest.FieldNameAr, opt => opt.Ignore())
            .ForMember(dest => dest.FieldNameEn, opt => opt.Ignore());

        CreateMap<CreateUpdateCourseCatalogDto, CourseCatalog>();

        // CourseProposal
        CreateMap<CourseProposal, CourseProposalDto>()
            .ForMember(dest => dest.FieldNameAr, opt => opt.Ignore())
            .ForMember(dest => dest.ProposedByName, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTimeFormatted, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewedAtFormatted, opt => opt.Ignore());

        CreateMap<CreateCourseProposalDto, CourseProposal>();

        // TenantCourse
        CreateMap<TenantCourse, TenantCourseDto>()
            .ForMember(dest => dest.CatalogCourseNameAr, opt => opt.Ignore())
            .ForMember(dest => dest.CatalogCourseNameEn, opt => opt.Ignore())
            .ForMember(dest => dest.CatalogCourseFieldNameAr, opt => opt.Ignore())
            .ForMember(dest => dest.CatalogCourseCategory, opt => opt.Ignore())
            .ForMember(dest => dest.AddedByName, opt => opt.Ignore())
            .ForMember(dest => dest.AddedAtFormatted, opt => opt.Ignore());

        CreateMap<UpdateTenantCourseDto, TenantCourse>();
    }
}
