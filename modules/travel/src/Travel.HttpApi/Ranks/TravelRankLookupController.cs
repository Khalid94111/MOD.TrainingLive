using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Travel.Ranks;

[Area(TravelRemoteServiceConsts.ModuleName)]
[RemoteService(Name = TravelRemoteServiceConsts.RemoteServiceName)]
[Route("api/travel/ranks")]
public class TravelRankLookupController : TravelController, ITravelRankLookupAppService
{
    private readonly ITravelRankLookupAppService _appService;

    public TravelRankLookupController(ITravelRankLookupAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public virtual Task<List<RankLookupDto>> GetRanksAsync()
    {
        return _appService.GetRanksAsync();
    }

    [HttpGet("{id}")]
    public virtual Task<RankLookupDto?> GetRankByIdAsync(Guid id)
    {
        return _appService.GetRankByIdAsync(id);
    }
}
