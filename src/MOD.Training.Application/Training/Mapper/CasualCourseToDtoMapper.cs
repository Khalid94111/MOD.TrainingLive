using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.CasualCourses.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CasualCourseToDtoMapper : MapperBase<CasualCourse, CasualCourseDto>
{
    [MapperIgnoreTarget(nameof(CasualCourseDto.CourseNameAr))]
    [MapperIgnoreTarget(nameof(CasualCourseDto.UnitName))]
    [MapperIgnoreTarget(nameof(CasualCourseDto.RequesterName))]
    [MapperIgnoreTarget(nameof(CasualCourseDto.NomineesCount))]
    [MapperIgnoreTarget(nameof(CasualCourseDto.FinancialsCount))]
    [MapperIgnoreTarget(nameof(CasualCourseDto.LatestReturnReason))]
    public override partial CasualCourseDto Map(CasualCourse source);
    public override partial void Map(CasualCourse source, CasualCourseDto destination);
}
