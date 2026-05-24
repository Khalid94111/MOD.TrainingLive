using MOD.Training.Training.Plans.Dtos;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Plans;

// Phase 4C-α (v4.10.0) — full contract for CourseSession execution endpoints. Source-agnostic:
// works for both annual-plan-sourced sessions (Phase 4C-α) and (future) center-plan-sourced
// sessions (Phase 4C-β). Creation lives elsewhere — AnnualPlanSessionAppService.CreateInternal/External.
public interface ICourseSessionAppService : IApplicationService
{
    Task<CourseSessionDetailDto> GetAsync(Guid id);

    Task<PagedResultDto<CourseSessionDto>> GetListAsync(CourseSessionGetListInput input);

    /// <summary>
    /// Atomic: flips IsSelected on the chosen PriceQuote (and off on any previous winner),
    /// writes ActualStartDate/EndDate, and transitions Status Planned → Scheduled in one
    /// unit of work. Only valid when current Status is Planned (external sessions).
    /// </summary>
    Task<CourseSessionDetailDto> SelectPriceQuoteAsync(Guid id, SelectSessionPriceQuoteDto input);

    /// <summary>Status Scheduled → InProgress. Staff transitions manually when training begins.</summary>
    Task<CourseSessionDetailDto> MarkInProgressAsync(Guid id);

    /// <summary>Status InProgress → Completed. Staff transitions when training ends.</summary>
    Task<CourseSessionDetailDto> MarkCompletedAsync(Guid id);

    /// <summary>Cancels a session before it starts. Allowed only in Planned or Scheduled status.</summary>
    Task<CourseSessionDetailDto> CancelAsync(Guid id, CancelSessionDto input);
}
