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
[Route("api/travel/clothing-allowance-rules")]
[Authorize(TravelManagementPermissions.ClothingAllowanceRules.Default)]
public class ClothingAllowanceRuleController : TravelController, IClothingAllowanceRuleAppService
{
    private readonly IClothingAllowanceRuleAppService _appService;

    public ClothingAllowanceRuleController(IClothingAllowanceRuleAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<ClothingAllowanceRuleDto>> GetListAsync(GetClothingAllowanceRuleListInput input)
    {
        return _appService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<ClothingAllowanceRuleDto> GetAsync(Guid id)
    {
        return _appService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(TravelManagementPermissions.ClothingAllowanceRules.Create)]
    public virtual Task<ClothingAllowanceRuleDto> CreateAsync(CreateUpdateClothingAllowanceRuleDto input)
    {
        return _appService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(TravelManagementPermissions.ClothingAllowanceRules.Edit)]
    public virtual Task<ClothingAllowanceRuleDto> UpdateAsync(Guid id, CreateUpdateClothingAllowanceRuleDto input)
    {
        return _appService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(TravelManagementPermissions.ClothingAllowanceRules.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _appService.DeleteAsync(id);
    }
}
