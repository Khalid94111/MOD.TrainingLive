using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travel.Permissions;
using Volo.Abp;

namespace Travel.TravelRequests;

[Area(TravelRemoteServiceConsts.ModuleName)]
[RemoteService(Name = TravelRemoteServiceConsts.RemoteServiceName)]
[Route("api/travel/funding-source-vote-rules")]
[Authorize(TravelManagementPermissions.TravelRequests.ManageAllowances)]
public class TravelFundingSourceVoteRuleController : TravelController, ITravelFundingSourceVoteRuleAppService
{
    private readonly ITravelFundingSourceVoteRuleAppService _appService;

    public TravelFundingSourceVoteRuleController(ITravelFundingSourceVoteRuleAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public Task<List<TravelFundingSourceVoteRuleDto>> GetListAsync()
    {
        return _appService.GetListAsync();
    }

    [HttpPut("{id}")]
    [Authorize(TravelManagementPermissions.TravelRequests.ManageAllowances)]
    public Task<TravelFundingSourceVoteRuleDto> UpdateAsync(Guid id, UpdateTravelFundingSourceVoteRuleDto input)
    {
        return _appService.UpdateAsync(id, input);
    }
}
