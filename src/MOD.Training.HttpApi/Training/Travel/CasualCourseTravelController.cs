using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Travel.Dtos;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace MOD.Training.Training.Travel;

[Area("Training")]
[RemoteService(Name = "Training")]
[Route("api/training/casual-courses/{casualCourseId:guid}/travel")]
[Authorize(TrainingExecutionPermissions.TravelRequests.Default)]
public class CasualCourseTravelController(ICasualCourseTravelAppService appService) : AbpControllerBase
{
    [HttpGet]
    public Task<CasualCourseTravelDto> GetAsync(Guid casualCourseId)
        => appService.GetAsync(casualCourseId);

    [HttpPost("refresh")]
    public Task<CasualCourseTravelDto> RefreshAsync(Guid casualCourseId)
        => appService.RefreshAsync(casualCourseId);

    [HttpPost("send")]
    [Authorize(TrainingExecutionPermissions.TravelRequests.Send)]
    public Task<CasualCourseTravelDto> SendAsync(Guid casualCourseId)
        => appService.SendAsync(casualCourseId);
}
