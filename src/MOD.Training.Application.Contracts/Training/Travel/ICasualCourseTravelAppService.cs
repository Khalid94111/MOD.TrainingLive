using System;
using System.Threading.Tasks;
using MOD.Training.Training.Travel.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Travel;

public interface ICasualCourseTravelAppService : IApplicationService
{
    Task<CasualCourseTravelDto> GetAsync(Guid casualCourseId);
    Task<CasualCourseTravelDto> RefreshAsync(Guid casualCourseId);
    Task<CasualCourseTravelDto> SendAsync(Guid casualCourseId);
}
