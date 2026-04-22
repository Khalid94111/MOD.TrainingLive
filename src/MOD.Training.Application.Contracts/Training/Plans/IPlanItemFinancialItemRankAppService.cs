using MOD.Training.Training.Plans.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Plans;

public interface IPlanItemFinancialItemRankAppService : IApplicationService
{
    Task<List<PlanItemFinancialItemRankDto>> GetListByPifiAsync(Guid pifiId);
    Task UpdateRateAsync(Guid rankRowId, UpdateRateDto input);
}
