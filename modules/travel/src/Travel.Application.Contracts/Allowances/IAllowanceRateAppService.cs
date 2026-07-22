using System;
using Volo.Abp.Application.Services;

namespace Travel.Allowances;

public interface IAllowanceRateAppService : ICrudAppService<
    AllowanceRateDto,
    Guid,
    GetAllowanceRateListInput,
    CreateUpdateAllowanceRateDto>
{
}
