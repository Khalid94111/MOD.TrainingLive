using System;
using Volo.Abp.Application.Services;

namespace Travel.Allowances;

public interface IAccommodationRuleAppService : ICrudAppService<
    AccommodationRuleDto,
    Guid,
    GetAccommodationRuleListInput,
    CreateUpdateAccommodationRuleDto>
{
}
