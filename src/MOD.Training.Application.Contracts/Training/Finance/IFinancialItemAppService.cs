using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MOD.Training.Training.Finance.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Finance;

public interface IFinancialItemAppService : ICrudAppService<
    FinancialItemDto,
    Guid,
    FinancialItemGetListInput,
    CreateUpdateFinancialItemDto>
{
    Task<List<FinancialItemDto>> GetSubItemsAsync();
}
