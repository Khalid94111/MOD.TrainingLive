using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.CasualCourses.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CasualCourseNominationToDtoMapper : MapperBase<CasualCourseNomination, CasualCourseNominationDto>
{
    [MapperIgnoreTarget(nameof(CasualCourseNominationDto.ServiceNumber))]
    [MapperIgnoreTarget(nameof(CasualCourseNominationDto.EmployeeName))]
    [MapperIgnoreTarget(nameof(CasualCourseNominationDto.RankName))]
    [MapperIgnoreTarget(nameof(CasualCourseNominationDto.UnitName))]
    [MapperIgnoreTarget(nameof(CasualCourseNominationDto.LastReturnNote))]
    public override partial CasualCourseNominationDto Map(CasualCourseNomination source);
    [MapperIgnoreTarget(nameof(CasualCourseNominationDto.ServiceNumber))]
    public override partial void Map(CasualCourseNomination source, CasualCourseNominationDto destination);
}
