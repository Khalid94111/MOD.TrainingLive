using MOD.Training.Training.AnnualPlanSessions.Dtos;
using MOD.Training.Training.Plans.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.AnnualPlanSessions;

// Phase 4C-α (v4.10.0) — Annual Plan Session creation + dashboard endpoints.
// Per §4.1, this service is ADDITIVE to CourseSessionAppService — it adds the
// annual-plan-source-specific operations (queue, create-internal, create-external,
// substitutes, dashboard). CourseSessionAppService (step 5) handles execution
// transitions (SelectPriceQuote, MarkInProgress, MarkCompleted, Cancel) generically
// regardless of source.
public interface IAnnualPlanSessionAppService : IApplicationService
{
    Task<PagedResultDto<PlanItemQueueItemDto>> GetPlanItemsQueueAsync(PlanItemQueueGetListInput input);

    Task<CourseSessionDto> CreateInternalSessionAsync(CreateInternalSessionDto input);

    Task<CourseSessionDto> CreateExternalSessionAsync(CreateExternalSessionDto input);

    Task<List<AvailableSubstituteDto>> GetAvailableSubstitutesAsync(Guid planItemId, Guid originalEmployeeId);

    Task<AnnualPlanProgressDto> GetProgressDashboardAsync(int? year);
}
