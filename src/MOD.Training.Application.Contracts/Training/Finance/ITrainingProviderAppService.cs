using MOD.Training.Training.Finance.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Finance;

public interface ITrainingProviderAppService : ICrudAppService<
    TrainingProviderDto,
    Guid,
    TrainingProviderGetListInput,
    CreateUpdateTrainingProviderDto>
{
    Task<List<TrainingProviderDto>> GetAllActiveAsync();
}
