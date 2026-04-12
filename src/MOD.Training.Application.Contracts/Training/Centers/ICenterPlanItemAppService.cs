using System;
using System.Threading.Tasks;
using MOD.Training.Training.Centers.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Centers;

public interface ICenterPlanItemAppService : ICrudAppService<
    TrainingCenterPlanItemDto,
    Guid,
    CenterPlanItemGetListInput,
    CreateUpdateCenterPlanItemDto>
{
    Task<TrainingCenterPlanItemDto> SetUnitsAsync(Guid id, SetPlanItemUnitsDto input);
}
