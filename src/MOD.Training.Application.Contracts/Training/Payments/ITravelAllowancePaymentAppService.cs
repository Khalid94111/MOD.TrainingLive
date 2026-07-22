using System;
using System.Threading.Tasks;
using MOD.Training.Training.Payments.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Payments;

/// <summary>
/// Read-only view of the confirmed financial results imported from Travel.
/// Travel is the only writer for these records.
/// </summary>
public interface ITravelAllowancePaymentAppService : IApplicationService
{
    Task<TravelAllowancePaymentDto> GetAsync(Guid id);

    Task<PagedResultDto<TravelAllowancePaymentDto>> GetListAsync(
        TravelAllowancePaymentGetListInput input);
}
