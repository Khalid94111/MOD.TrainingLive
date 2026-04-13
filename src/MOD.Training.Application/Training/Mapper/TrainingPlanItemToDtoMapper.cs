using MOD.Training.Training.Plans;
using MOD.Training.Training.Plans.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TrainingPlanItemToDtoMapper : MapperBase<TrainingPlanItem, TrainingPlanItemDto>
{
    public override partial TrainingPlanItemDto Map(TrainingPlanItem source);
    public override partial void Map(TrainingPlanItem source, TrainingPlanItemDto destination);
}
