using MOD.Training.Training.Execution;
using MOD.Training.Training.Execution.Dtos;
using Riok.Mapperly.Abstractions;
using System;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CreateUpdateTravelInstructionToEntityMapper : MapperBase<CreateUpdateTravelInstructionDto, TravelInstruction>
{
    public override TravelInstruction Map(CreateUpdateTravelInstructionDto source)
    {
        var entity = new TravelInstruction(Guid.NewGuid());
        Map(source, entity);
        return entity;
    }

    [MapperIgnoreTarget(nameof(TravelInstruction.Id))]
    [MapperIgnoreTarget(nameof(TravelInstruction.TenantId))]
    [MapperIgnoreTarget(nameof(TravelInstruction.Status))]
    [MapperIgnoreTarget(nameof(TravelInstruction.CalculatedTravelDays))]
    [MapperIgnoreTarget(nameof(TravelInstruction.ExtraProperties))]
    [MapperIgnoreTarget(nameof(TravelInstruction.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(TravelInstruction.CreationTime))]
    [MapperIgnoreTarget(nameof(TravelInstruction.CreatorId))]
    [MapperIgnoreTarget(nameof(TravelInstruction.LastModificationTime))]
    [MapperIgnoreTarget(nameof(TravelInstruction.LastModifierId))]
    [MapperIgnoreTarget(nameof(TravelInstruction.IsDeleted))]
    [MapperIgnoreTarget(nameof(TravelInstruction.DeleterId))]
    [MapperIgnoreTarget(nameof(TravelInstruction.DeletionTime))]
    public override partial void Map(CreateUpdateTravelInstructionDto source, TravelInstruction destination);
}
