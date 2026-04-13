using MOD.Training.Training.Finance.Dtos;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Finance;

public interface IPriceQuoteAppService : ICrudAppService<
    PriceQuoteDto,
    Guid,
    PriceQuoteGetListInput,
    CreateUpdatePriceQuoteDto>
{
    Task ApproveAsync(Guid id);
    Task RejectAsync(Guid id);
}
