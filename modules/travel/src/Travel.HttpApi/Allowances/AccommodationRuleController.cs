using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travel.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace Travel.Allowances;

[Area(TravelRemoteServiceConsts.ModuleName)]
[RemoteService(Name = TravelRemoteServiceConsts.RemoteServiceName)]
[Route("api/travel/accommodation-rules")]
[Authorize(TravelManagementPermissions.AccommodationRules.Default)]
public class AccommodationRuleController : TravelController, IAccommodationRuleAppService
{
    private readonly IAccommodationRuleAppService _appService;

    public AccommodationRuleController(IAccommodationRuleAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public Task<PagedResultDto<AccommodationRuleDto>> GetListAsync(GetAccommodationRuleListInput input)
    {
        return _appService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public Task<AccommodationRuleDto> GetAsync(Guid id)
    {
        return _appService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(TravelManagementPermissions.AccommodationRules.Create)]
    public Task<AccommodationRuleDto> CreateAsync(CreateUpdateAccommodationRuleDto input)
    {
        return _appService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(TravelManagementPermissions.AccommodationRules.Edit)]
    public Task<AccommodationRuleDto> UpdateAsync(Guid id, CreateUpdateAccommodationRuleDto input)
    {
        return _appService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(TravelManagementPermissions.AccommodationRules.Delete)]
    public Task DeleteAsync(Guid id)
    {
        return _appService.DeleteAsync(id);
    }
}
