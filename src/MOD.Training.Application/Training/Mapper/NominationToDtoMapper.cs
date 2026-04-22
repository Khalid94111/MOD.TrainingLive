using MOD.Training.Training.Nominations;
using MOD.Training.Training.Nominations.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class NominationToDtoMapper : MapperBase<Nomination, NominationDto>
{
    [MapperIgnoreTarget(nameof(NominationDto.SessionCode))]
    [MapperIgnoreTarget(nameof(NominationDto.CourseName))]
    [MapperIgnoreTarget(nameof(NominationDto.EmployeeName))]
    [MapperIgnoreTarget(nameof(NominationDto.NominatedByName))]
    public override partial NominationDto Map(Nomination source);
    public override partial void Map(Nomination source, NominationDto destination);
}
