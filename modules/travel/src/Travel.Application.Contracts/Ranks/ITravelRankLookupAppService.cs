using Travel.Allowances;
using Volo.Abp.Application.Services;

namespace Travel.Ranks;

public interface ITravelRankLookupAppService : IApplicationService, IRankLookup
{
}
