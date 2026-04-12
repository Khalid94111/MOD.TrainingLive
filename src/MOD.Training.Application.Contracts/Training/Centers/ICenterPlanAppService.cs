using System;
using System.Threading.Tasks;
using MOD.Training.Training.Centers.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Centers;

public interface ICenterPlanAppService : ICrudAppService<
    TrainingCenterPlanDto,
    Guid,
    CenterPlanGetListInput,
    CreateUpdateCenterPlanDto>
{
    Task<TrainingCenterPlanDto> SubmitAsync(Guid id);
    Task<TrainingCenterPlanDto> ApproveAsync(Guid id);
    Task<TrainingCenterPlanDto> RejectAsync(Guid id);
    Task<TrainingCenterPlanDto> ReturnAsync(Guid id);
}
