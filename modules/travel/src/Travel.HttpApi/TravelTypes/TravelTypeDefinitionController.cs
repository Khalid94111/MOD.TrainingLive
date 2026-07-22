using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travel.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace Travel.TravelTypes;

[Area(TravelRemoteServiceConsts.ModuleName)]
[RemoteService(Name = TravelRemoteServiceConsts.RemoteServiceName)]
[Route("api/travel/travel-types")]
[Authorize(TravelManagementPermissions.TravelTypes.Default)]
public class TravelTypeDefinitionController : TravelController, ITravelTypeDefinitionAppService
{
    private readonly ITravelTypeDefinitionAppService _appService;

    public TravelTypeDefinitionController(ITravelTypeDefinitionAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public Task<PagedResultDto<TravelTypeDefinitionDto>> GetListAsync(GetTravelTypeDefinitionListInput input)
    {
        return _appService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public Task<TravelTypeDefinitionDto> GetAsync(Guid id)
    {
        return _appService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(TravelManagementPermissions.TravelTypes.Create)]
    public Task<TravelTypeDefinitionDto> CreateAsync(CreateUpdateTravelTypeDefinitionDto input)
    {
        return _appService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(TravelManagementPermissions.TravelTypes.Edit)]
    public Task<TravelTypeDefinitionDto> UpdateAsync(Guid id, CreateUpdateTravelTypeDefinitionDto input)
    {
        return _appService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(TravelManagementPermissions.TravelTypes.Delete)]
    public Task DeleteAsync(Guid id)
    {
        return _appService.DeleteAsync(id);
    }
}
