using AutoMapper;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Finance.Dtos;

namespace MOD.Training.Training;

public class TrainingAutoMapperProfile : Profile
{
    public TrainingAutoMapperProfile()
    {
        // CourseTypeFinancialItemDefault
        CreateMap<CourseTypeFinancialItemDefault, CourseTypeFinancialItemDefaultDto>()
            .ForMember(dest => dest.FinancialItemNameAr, opt => opt.Ignore())
            .ForMember(dest => dest.FinancialItemNameEn, opt => opt.Ignore())
            .ForMember(dest => dest.FinancialItemCode, opt => opt.Ignore());

        CreateMap<CreateCourseTypeFinancialItemDefaultDto, CourseTypeFinancialItemDefault>();
    }
}
