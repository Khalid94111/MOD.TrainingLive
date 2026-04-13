using MOD.Training.Training.Finance.Dtos;
using MOD.Training.Training.Plans;
using MOD.Training.Training.Plans.Dtos;
using Riok.Mapperly.Abstractions;
using System;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CreateUpdateTrainingPlanToEntityMapper : MapperBase<CreateUpdateTrainingPlanDto, TrainingPlan>
{
     public override   TrainingPlan Map(CreateUpdateTrainingPlanDto source)
    {
        var entity = new TrainingPlan(Guid.NewGuid(), source.Year);
        Map(source, entity);
        return entity;
    }
    public override partial void Map(CreateUpdateTrainingPlanDto source, TrainingPlan destination);
}
