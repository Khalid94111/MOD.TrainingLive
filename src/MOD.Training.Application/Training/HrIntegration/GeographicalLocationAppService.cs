using MOD.Training.Training.Hr;
using MOD.Training.Training.HrIntegration.Dtos;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.HrIntegration;

/// <summary>
/// Read-only HR cascade for Country + City dropdowns. Phase 4B-α.
/// No create / update / delete — HR module owns the data.
/// </summary>
public class GeographicalLocationAppService(
    IRepository<GeographicalLocation, Guid> repository,
    GeographicalLocationToDtoMapper toDtoMapper)
    : ApplicationService, IGeographicalLocationAppService
{
    public async Task<ListResultDto<GeographicalLocationDto>> GetCountriesAsync()
    {
        var queryable = await repository.GetQueryableAsync();
        var entities = await AsyncExecuter.ToListAsync(
            queryable.Where(x => x.LocationParentId == null)
                     .OrderBy(x => x.ArabicName));
        return new ListResultDto<GeographicalLocationDto>(
            entities.Select(toDtoMapper.Map).ToList());
    }

    public async Task<ListResultDto<GeographicalLocationDto>> GetCitiesAsync(Guid countryId)
    {
        var queryable = await repository.GetQueryableAsync();
        var entities = await AsyncExecuter.ToListAsync(
            queryable.Where(x => x.LocationParentId == countryId)
                     .OrderBy(x => x.ArabicName));
        return new ListResultDto<GeographicalLocationDto>(
            entities.Select(toDtoMapper.Map).ToList());
    }
}
