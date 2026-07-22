using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Travel.Allowances;
using Volo.Abp.Application.Services;

namespace Travel.Ranks;

public class TravelRankLookupAppService : ApplicationService, ITravelRankLookupAppService
{
    private readonly IRankLookup _rankLookup;

    public TravelRankLookupAppService(IRankLookup rankLookup)
    {
        _rankLookup = rankLookup;
    }

    public Task<List<RankLookupDto>> GetRanksAsync()
    {
        return _rankLookup.GetRanksAsync();
    }

    public Task<RankLookupDto?> GetRankByIdAsync(Guid id)
    {
        return _rankLookup.GetRankByIdAsync(id);
    }
}
