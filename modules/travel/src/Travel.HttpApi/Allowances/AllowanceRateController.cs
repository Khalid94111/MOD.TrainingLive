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
[Route("api/travel/allowance-rates")]
[Authorize(TravelManagementPermissions.AllowanceRates.Default)]
public class AllowanceRateController : TravelController, IAllowanceRateAppService
{
    private readonly IAllowanceRateAppService _appService;

    public AllowanceRateController(IAllowanceRateAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<AllowanceRateDto>> GetListAsync(GetAllowanceRateListInput input)
    {
        return _appService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<AllowanceRateDto> GetAsync(Guid id)
    {
        return _appService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(TravelManagementPermissions.AllowanceRates.Create)]
    public virtual Task<AllowanceRateDto> CreateAsync(CreateUpdateAllowanceRateDto input)
    {
        return _appService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(TravelManagementPermissions.AllowanceRates.Edit)]
    public virtual Task<AllowanceRateDto> UpdateAsync(Guid id, CreateUpdateAllowanceRateDto input)
    {
        return _appService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(TravelManagementPermissions.AllowanceRates.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _appService.DeleteAsync(id);
    }
}
