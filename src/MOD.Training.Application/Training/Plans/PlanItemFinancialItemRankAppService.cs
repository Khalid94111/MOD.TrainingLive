using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Plans;

[Authorize(TrainingPermissions.PlanItemFinancialItemRank.Default)]
public class PlanItemFinancialItemRankAppService(
    IRepository<PlanItemFinancialItemRank, Guid> repository,
    EmployeeResolver employeeResolver,
    PlanItemRankBreakdownManager rankBreakdownManager,
    PlanItemFinancialItemRankToDtoMapper toDtoMapper)
    : ApplicationService, IPlanItemFinancialItemRankAppService
{
    public async Task<List<PlanItemFinancialItemRankDto>> GetListByPifiAsync(Guid pifiId)
    {
        var queryable = await repository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(
            queryable.Where(x => x.PlanItemFinancialItemId == pifiId));

        var ranks = await employeeResolver.GetAllRanksAsync();
        var rankLookup = ranks.ToDictionary(r => r.Id);

        return items.Select(e =>
        {
            var dto = toDtoMapper.Map(e);
            if (rankLookup.TryGetValue(e.RankId, out var rank))
            {
                dto.RankNameAr = rank.NameAr;
                dto.RankNameEn = rank.NameEn;
            }
            return dto;
        }).ToList();
    }

    [Authorize(TrainingPermissions.PlanItemFinancialItemRank.UpdateRate)]
    public async Task UpdateRateAsync(Guid rankRowId, UpdateRateDto input)
    {
        await rankBreakdownManager.UpdateRateAsync(rankRowId, input.NewRate);
    }
}
