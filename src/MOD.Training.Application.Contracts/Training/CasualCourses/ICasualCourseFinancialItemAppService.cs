using MOD.Training.Training.CasualCourses.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.CasualCourses;

public interface ICasualCourseFinancialItemAppService : IApplicationService
{
    Task<List<CasualCourseFinancialItemDto>> GetListByCasualCourseAsync(Guid casualCourseId);

    /// <summary>
    /// Auto-fills CasualCourseFinancialItems from CourseTypeFinancialItemDefaults. Patch 5
    /// moved the trigger out of <c>CreateAsync</c> into
    /// <c>CasualCourseAppService.AssignScenarioAsync</c>: rows are created the first time
    /// Staff picks a scenario, so the funding scenario is already set on the course and
    /// <see cref="MOD.Training.Training.Managers.FundingScenarioSourceResolver"/> can derive
    /// the correct Source for every row at insertion time (no placeholder phase).
    /// <para>
    /// runAsSystem = true: invoked from the AppService while still under Staff's permission
    /// boundary; skips the per-call permission check.
    /// </para>
    /// <para>
    /// <paramref name="courseCostSeed"/> — when provided and a course-cost financial item exists
    /// in the defaults, that row's rate is seeded from this value (UTM's CourseCost form input)
    /// and the rank row's RateSource is recorded as <c>"FromUTMForm"</c>. Pass null to use the
    /// catalog default.
    /// </para>
    /// </summary>
    Task<List<CasualCourseFinancialItemDto>> AutoFillFromDefaultsAsync(
        Guid casualCourseId,
        bool runAsSystem = false,
        decimal? courseCostSeed = null);

    Task<CasualCourseFinancialItemDto> AddItemAsync(Guid casualCourseId, CreateCasualCourseFinancialItemDto input);

    Task DeleteItemAsync(Guid id);
}
