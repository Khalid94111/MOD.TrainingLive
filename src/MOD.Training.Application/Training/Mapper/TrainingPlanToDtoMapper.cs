using MOD.Training.Training.Plans;
using MOD.Training.Training.Plans.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TrainingPlanToDtoMapper : MapperBase<TrainingPlan, TrainingPlanDto>
{
    public override partial TrainingPlanDto Map(TrainingPlan source);
    public override partial void Map(TrainingPlan source, TrainingPlanDto destination);
}
