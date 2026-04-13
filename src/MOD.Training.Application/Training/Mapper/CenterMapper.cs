using MOD.Training.Training.Centers;
using MOD.Training.Training.Centers.Dtos;
using Riok.Mapperly.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class TrainingCenterToDtoMapper : MapperBase<TrainingCenter, TrainingCenterDto>
    {
        public override partial TrainingCenterDto Map(TrainingCenter source);
        public override partial void Map(TrainingCenter source, TrainingCenterDto destination);
    }

    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class CreateUpdateTrainingCenterToEntityMapper : MapperBase<CreateUpdateTrainingCenterDto, TrainingCenter>
    {
        public override partial TrainingCenter Map(CreateUpdateTrainingCenterDto source);
        public override partial void Map(CreateUpdateTrainingCenterDto source, TrainingCenter destination);
    }

    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class CenterPlanWindowToDtoMapper : MapperBase<CenterPlanWindow, CenterPlanWindowDto>
    {
        public override partial CenterPlanWindowDto Map(CenterPlanWindow source);
        public override partial void Map(CenterPlanWindow source, CenterPlanWindowDto destination);
    }

    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class CreateUpdateCenterPlanWindowToEntityMapper : MapperBase<CreateUpdateCenterPlanWindowDto, CenterPlanWindow>
    {
        public override partial CenterPlanWindow Map(CreateUpdateCenterPlanWindowDto source);
        public override partial void Map(CreateUpdateCenterPlanWindowDto source, CenterPlanWindow destination);
    }
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class CenterRoleAssignmentToDtoMapper : MapperBase<CenterRoleAssignment, CenterRoleAssignmentDto>
    {
        public override partial CenterRoleAssignmentDto Map(CenterRoleAssignment source);
        public override partial void Map(CenterRoleAssignment source, CenterRoleAssignmentDto destination);
    }

    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class TrainingCenterPlanToDtoMapper : MapperBase<TrainingCenterPlan, TrainingCenterPlanDto>
    {
        public override partial TrainingCenterPlanDto Map(TrainingCenterPlan source);
        public override partial void Map(TrainingCenterPlan source, TrainingCenterPlanDto destination);
    }

    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class CreateUpdateCenterPlanToEntityMapper : MapperBase<CreateUpdateCenterPlanDto, TrainingCenterPlan>
    {
        public override partial TrainingCenterPlan Map(CreateUpdateCenterPlanDto source);
        public override partial void Map(CreateUpdateCenterPlanDto source, TrainingCenterPlan destination);
    }
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class TrainingCenterPlanItemToDtoMapper : MapperBase<TrainingCenterPlanItem, TrainingCenterPlanItemDto>
    {
        public override partial TrainingCenterPlanItemDto Map(TrainingCenterPlanItem source);
        public override partial void Map(TrainingCenterPlanItem source, TrainingCenterPlanItemDto destination);


    }

    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class CreateUpdateCenterPlanItemToEntityMapper : MapperBase<CreateUpdateCenterPlanItemDto, TrainingCenterPlanItem>
    {
        public override partial TrainingCenterPlanItem Map(CreateUpdateCenterPlanItemDto source);
        public override partial void Map(CreateUpdateCenterPlanItemDto source, TrainingCenterPlanItem destination);
    }


}
