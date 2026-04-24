using MOD.Training.Training.CasualCourses.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.CasualCourses;

public interface ICasualCourseFinancialAppService : IApplicationService
{
    Task<List<CasualCourseFinancialDto>> GetListByCasualCourseAsync(Guid casualCourseId);

    /// <summary>
    /// Auto-fills CasualCourseFinancials from CourseTypeFinancialItemDefaults.
    /// <para>
    /// runAsSystem = true: invoked by CasualCourseAppService.CreateAsync while the course
    /// is Draft. Scenario is not yet picked; rank rows use the scenario-agnostic default Source.
    /// </para>
    /// <para>
    /// runAsSystem = false: Staff invokes during UnderReview to add any missing defaults.
    /// Requires FundingScenario to already be set (via AssignScenarioAsync).
    /// </para>
    /// </summary>
    Task<List<CasualCourseFinancialDto>> AutoFillFromDefaultsAsync(Guid casualCourseId, bool runAsSystem = false);

    Task<CasualCourseFinancialDto> AddItemAsync(Guid casualCourseId, CreateCasualCourseFinancialDto input);

    Task DeleteItemAsync(Guid id);
}
