using MOD.Training.Training.Plans.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Plans;

public interface ITrainingPlanItemAppService : ICrudAppService<
    TrainingPlanItemDto,
    Guid,
    TrainingPlanItemGetListInput,
    CreateUpdateTrainingPlanItemDto>
{
    Task UpdateEstimatedCostAsync(Guid id, UpdateEstimatedCostDto input);
    Task<List<PlanItemConditionDto>> GetConditionsAsync(Guid planItemId);
}
