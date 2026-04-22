using MOD.Training.Training.Plans.Dtos;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Plans;

public interface ITrainingPlanAppService : ICrudAppService<
    TrainingPlanDto,
    Guid,
    TrainingPlanGetListInput,
    CreateUpdateTrainingPlanDto>
{
    Task OpenSubmissionWindowAsync(Guid id);
    Task CloseSubmissionWindowAsync(Guid id);
    Task SubmitForReviewAsync(Guid id);
    Task ApproveAsync(Guid id);
    Task FinalApproveAsync(Guid id);
    Task RejectAsync(Guid id, string? reason);
    Task ReturnToStaffAsync(Guid id, string? reason);

    // CHG-05
    Task ReturnToCreatorAsync(Guid id, ReturnReasonDto input);
    Task ResubmitAsync(Guid id);
}
