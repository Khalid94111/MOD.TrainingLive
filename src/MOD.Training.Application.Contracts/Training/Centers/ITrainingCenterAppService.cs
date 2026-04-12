using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MOD.Training.Training.Centers.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Centers;

public interface ITrainingCenterAppService : ICrudAppService<
    TrainingCenterDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateTrainingCenterDto>
{
    Task<List<CenterRoleAssignmentDto>> GetRoleAssignmentsAsync(Guid centerId);
    Task<List<CenterRoleAssignmentDto>> SetRoleAssignmentsAsync(Guid centerId, SetCenterRoleAssignmentsDto input);
}
