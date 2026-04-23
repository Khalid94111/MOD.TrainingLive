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
    Task<EstimatePreviewDto> GetEstimatePreviewAsync(EstimatePreviewInput input);

    Task<CasualCourseDetailDto> GetDetailAsync(Guid id);

    Task<CasualCourseDto> SubmitAsync(Guid id);
    Task<CasualCourseDto> UGMApproveAsync(Guid id, CreatePlanNoteDto? note);
    Task<CasualCourseDto> StartReviewAsync(Guid id);
    Task<CasualCourseDto> AssignScenarioAsync(Guid id, AssignScenarioDto input);
    Task<CasualCourseDto> TDApproveAsync(Guid id, CreatePlanNoteDto? note);
    Task<CasualCourseDto> HeadApproveAsync(Guid id, CreatePlanNoteDto? note);
    Task<CasualCourseDto> ReturnAsync(Guid id, ReturnReasonDto input);
    Task<CasualCourseDto> ResubmitAsync(Guid id);
    Task<CasualCourseDto> RejectAsync(Guid id, RejectDto input);
}
