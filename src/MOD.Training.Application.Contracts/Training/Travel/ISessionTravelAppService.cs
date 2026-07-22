using System;
using System.Threading.Tasks;
using MOD.Training.Training.Travel.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Travel;

public interface ISessionTravelAppService : IApplicationService
{
    Task<SessionTravelDto> GetAsync(Guid sessionId);
    Task<SessionTravelDto> RefreshAsync(Guid sessionId);
    Task<SessionTravelDto> SendAsync(Guid sessionId);
}
