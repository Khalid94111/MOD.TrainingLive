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
[Route("api/travel/allowance-rules")]
[Authorize(TravelManagementPermissions.AllowanceRules.Default)]
public class AllowanceRuleController : TravelController, IAllowanceRuleAppService
{
    private readonly IAllowanceRuleAppService _appService;

    public AllowanceRuleController(IAllowanceRuleAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<AllowanceRuleDto>> GetListAsync(GetAllowanceRuleListInput input)
    {
        return _appService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<AllowanceRuleDto> GetAsync(Guid id)
    {
        return _appService.GetAsync(id);
    }

    [HttpPost("{id}/test")]
    [Authorize(TravelManagementPermissions.AllowanceRules.Default)]
    public virtual Task<AllowanceRuleTestResultDto> TestAsync(Guid id, AllowanceRuleTestInputDto input)
    {
        return _appService.TestAsync(id, input);
    }

    [HttpPost]
    [Authorize(TravelManagementPermissions.AllowanceRules.Create)]
    public virtual Task<AllowanceRuleDto> CreateAsync(CreateUpdateAllowanceRuleDto input)
    {
        return _appService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(TravelManagementPermissions.AllowanceRules.Edit)]
    public virtual Task<AllowanceRuleDto> UpdateAsync(Guid id, CreateUpdateAllowanceRuleDto input)
    {
        return _appService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(TravelManagementPermissions.AllowanceRules.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _appService.DeleteAsync(id);
    }
}
