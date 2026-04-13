using MOD.Training.Training.Plans;
using MOD.Training.Training.Plans.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CourseSessionToDtoMapper : MapperBase<CourseSession, CourseSessionDto>
{
    public override partial CourseSessionDto Map(CourseSession source);
    public override partial void Map(CourseSession source, CourseSessionDto destination);
}
