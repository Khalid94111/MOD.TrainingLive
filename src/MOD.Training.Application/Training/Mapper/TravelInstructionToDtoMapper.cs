using MOD.Training.Training.Execution;
using MOD.Training.Training.Execution.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TravelInstructionToDtoMapper : MapperBase<TravelInstruction, TravelInstructionDto>
{
    [MapperIgnoreTarget(nameof(TravelInstructionDto.EffectiveTravelDays))]
    public override partial TravelInstructionDto Map(TravelInstruction source);

    [MapperIgnoreTarget(nameof(TravelInstructionDto.EffectiveTravelDays))]
    public override partial void Map(TravelInstruction source, TravelInstructionDto destination);
}
