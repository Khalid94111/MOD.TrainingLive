using System;
using MOD.Training.Training.Finance.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Finance;

public interface ITrainingBudgetAppService : ICrudAppService<
    TrainingBudgetDto,
    Guid,
    TrainingBudgetGetListInput,
    CreateUpdateTrainingBudgetDto>
{
}
