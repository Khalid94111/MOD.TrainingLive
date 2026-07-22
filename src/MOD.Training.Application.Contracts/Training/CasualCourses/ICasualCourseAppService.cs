using MOD.Training.Training.CasualCourses.Dtos;
using MOD.Training.Training.Plans.Dtos;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.CasualCourses;

public interface ICasualCourseAppService : ICrudAppService<
    CasualCourseDto,
    Guid,
    CasualCourseGetListInput,
    CreateUpdateCasualCourseDto>
{
    Task<CasualCourseDetailDto> GetDetailAsync(Guid id);

    /// <summary>
    /// Patch 5 — server-side calculator. Returns the projected cost breakdown for a casual
    /// course request without touching the database. Drives PAGE 4.2 Section E (UTM, live
    /// updates) and PAGE 4.4 UGM variant (static, on page load). FundingScenario is not part
    /// of the input — it isn't picked yet at this stage.
    /// </summary>
    Task<CalculatePreviewDto> CalculatePreviewAsync(CalculatePreviewInput input);

    Task<CasualCourseDto> SubmitAsync(Guid id);
    Task<CasualCourseDto> UGMApproveAsync(Guid id, CreatePlanNoteDto? note);
    Task<CasualCourseDto> StartReviewAsync(Guid id);
    Task<CasualCourseDto> AssignScenarioAsync(Guid id, AssignScenarioDto input);
    Task<CasualCourseDto> TDApproveAsync(Guid id, CreatePlanNoteDto? note);
    Task<CasualCourseDto> HeadApproveAsync(Guid id, CreatePlanNoteDto? note);
    Task<CasualCourseDto> ReturnAsync(Guid id, ReturnReasonDto input);
    Task<CasualCourseDto> ResubmitAsync(Guid id);
    Task<CasualCourseDto> RejectAsync(Guid id, RejectDto input);

    /// <summary>
    /// Phase 4B-α — atomic quote-winner pick. Course must be THApproved. Flips
    /// <c>IsSelected</c> on previous winner (if any), writes <c>ActualStartDate</c> +
    /// <c>ActualEndDate</c>, and points <c>SelectedPriceQuoteId</c> at the new quote.
    /// </summary>
    Task<CasualCourseDto> SelectPriceQuoteAsync(Guid id, SelectPriceQuoteDto input);

    /// <summary>Moves an execution-ready course from Scheduled to InProgress.</summary>
    Task<CasualCourseDto> MarkInProgressAsync(Guid id);

    /// <summary>Moves an active course from InProgress to Completed.</summary>
    Task<CasualCourseDto> MarkCompletedAsync(Guid id);
}
