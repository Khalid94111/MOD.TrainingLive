using MOD.Training.Training.CasualCourses.Dtos;
using MOD.Training.Training.Enums;
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
    /// The scenario is committed to the course as a side effect — the picker on PAGE 4.3
    /// updates a local signal only, and clicking Auto-fill is the moment Staff confirms it.
    /// </summary>
    Task<List<CasualCourseFinancialDto>> AutoFillFromDefaultsAsync(Guid casualCourseId, FundingScenario scenario);

    Task<CasualCourseFinancialDto> AddItemAsync(Guid casualCourseId, CreateCasualCourseFinancialDto input);

    Task DeleteItemAsync(Guid id);
}
