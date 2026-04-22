using MOD.Training.Training.Plans;
using MOD.Training.Training.Plans.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class PlanNoteToDtoMapper : MapperBase<PlanNote, PlanNoteDto>
{
    [MapperIgnoreTarget(nameof(PlanNoteDto.CreatedByName))]
    public override partial PlanNoteDto Map(PlanNote source);
    public override partial void Map(PlanNote source, PlanNoteDto destination);
}
