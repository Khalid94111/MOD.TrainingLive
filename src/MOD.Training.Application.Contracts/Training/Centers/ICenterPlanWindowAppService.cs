using System;
using MOD.Training.Training.Centers.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Centers;

public interface ICenterPlanWindowAppService : ICrudAppService<
    CenterPlanWindowDto,
    Guid,
    CenterPlanWindowGetListInput,
    CreateUpdateCenterPlanWindowDto>
{
}
