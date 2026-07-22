using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Travel.Ranks;

namespace Travel.Allowances;

public interface IRankLookup
{
    Task<List<RankLookupDto>> GetRanksAsync();
    Task<RankLookupDto?> GetRankByIdAsync(Guid id);
}
