using System;
using System.Threading.Tasks;
using MOD.Training.Training.Payments.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Payments;

public interface ITrainingExpenseRecoveryAppService : IApplicationService
{
    Task<TrainingExpenseRecoveryDto> GetAsync(Guid id);
    Task<PagedResultDto<TrainingExpenseRecoveryDto>> GetListAsync(TrainingExpenseRecoveryGetListInput input);
    Task<int> RefreshAsync();
    Task<TrainingExpenseRecoveryDto> MarkReviewedAsync(Guid id, MarkTrainingExpenseRecoveryReviewedDto input);
    Task<TrainingExpenseRecoveryDto> MarkItemSettledAsync(
        Guid id,
        Guid itemId,
        MarkTrainingExpenseRecoverySettledDto input);
    Task<TrainingExpenseRecoveryDto> MarkAllSettledAsync(
        Guid id,
        MarkTrainingExpenseRecoverySettledDto input);
}
