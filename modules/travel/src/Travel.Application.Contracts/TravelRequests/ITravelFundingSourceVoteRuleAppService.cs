using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Travel.TravelRequests;

public interface ITravelFundingSourceVoteRuleAppService : IApplicationService
{
    Task<List<TravelFundingSourceVoteRuleDto>> GetListAsync();
    Task<TravelFundingSourceVoteRuleDto> UpdateAsync(Guid id, UpdateTravelFundingSourceVoteRuleDto input);
}
