using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MOD.Training.Training.Finance.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Finance;

public interface ITrainingBudgetAppService : IApplicationService
{
    Task<TrainingBudgetDto> GetAsync(Guid id);
    Task<PagedResultDto<TrainingBudgetDto>> GetListAsync(TrainingBudgetGetListInput input);
    Task<List<int>> GetYearsAsync();
    Task<TrainingBudgetDto> UpdateAsync(Guid id, UpdateAlertThresholdDto input);
    Task<TrainingBudgetDto> SetThresholdAsync(
        Guid financialItemId,
        int year,
        UpdateAlertThresholdDto input);
}
