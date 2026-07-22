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
[Route("api/training/sessions/{sessionId:guid}/travel")]
[Authorize(TrainingExecutionPermissions.TravelRequests.Default)]
public class SessionTravelController(ISessionTravelAppService appService) : AbpControllerBase
{
    [HttpGet]
    public Task<SessionTravelDto> GetAsync(Guid sessionId)
        => appService.GetAsync(sessionId);

    [HttpPost("refresh")]
    public Task<SessionTravelDto> RefreshAsync(Guid sessionId)
        => appService.RefreshAsync(sessionId);

    [HttpPost("send")]
    [Authorize(TrainingExecutionPermissions.TravelRequests.Send)]
    public Task<SessionTravelDto> SendAsync(Guid sessionId)
        => appService.SendAsync(sessionId);
}
