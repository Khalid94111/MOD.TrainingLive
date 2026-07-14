using MOD.Training.Training.Plans.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Plans;

public interface IPlanItemFinancialItemAppService : IApplicationService
{
    Task<List<PlanItemFinancialItemDto>> GetListByPlanItemAsync(Guid planItemId);
    Task<PlanItemFinancialItemDto> CreateAsync(CreateUpdatePlanItemFinancialItemDto input);
    Task<PlanItemFinancialItemDto> UpdateAsync(Guid id, CreateUpdatePlanItemFinancialItemDto input);
    Task DeleteAsync(Guid id);
}
