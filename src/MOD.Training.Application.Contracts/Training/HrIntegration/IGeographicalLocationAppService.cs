using MOD.Training.Training.HrIntegration.Dtos;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.HrIntegration;

/// <summary>
/// Read-only HR cascade for Country + City dropdowns. No create / update / delete —
/// HR module owns the data; this service only reads.
/// </summary>
public interface IGeographicalLocationAppService : IApplicationService
{
    Task<ListResultDto<GeographicalLocationDto>> GetCountriesAsync();
    Task<ListResultDto<GeographicalLocationDto>> GetCitiesAsync(Guid countryId);
}
