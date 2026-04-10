using System;
using System.Threading.Tasks;
using MOD.Training.Training.Finance.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Finance;

public interface IExchangeRateAppService : IApplicationService
{
    Task<PagedResultDto<ExchangeRateDto>> GetListAsync(ExchangeRateGetListInput input);
    Task<ExchangeRateDto> GetActiveAsync();
    Task<ExchangeRateDto> CreateAsync(CreateExchangeRateDto input);
    Task DeleteAsync(Guid id);
}
