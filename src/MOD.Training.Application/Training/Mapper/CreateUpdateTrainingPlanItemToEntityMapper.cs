using MOD.Training.Training.Plans;
using MOD.Training.Training.Plans.Dtos;
using Riok.Mapperly.Abstractions;
using System;
using Volo.Abp.Mapperly;
using Volo.Abp.Users;

namespace MOD.Training.Training;


[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CreateUpdateTrainingPlanItemToEntityMapper : MapperBase<CreateUpdateTrainingPlanItemDto, TrainingPlanItem>
{
    private readonly ICurrentUser _currentUser;

    public override   TrainingPlanItem Map(CreateUpdateTrainingPlanItemDto source)
    {
        var entity = new TrainingPlanItem(Guid.NewGuid(), source.PlanId, source.TenantCourseId,source.CourseType, source.PreferredQuarter,
        source.Priority, source.Justification,  _currentUser.Id.Value  );
        Map(source, entity);
        return entity;
    }
    public override partial void Map(CreateUpdateTrainingPlanItemDto source, TrainingPlanItem destination);
}
