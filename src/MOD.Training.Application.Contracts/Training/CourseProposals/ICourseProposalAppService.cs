using MOD.Training.Training.CourseProposals.Dtos;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.CourseProposals;

public interface ICourseProposalAppService : IApplicationService
{
    Task<CourseProposalDto> GetAsync(Guid id);
    Task<PagedResultDto<CourseProposalDto>> GetListAsync(CourseProposalGetListInput input);
    Task<CourseProposalDto> CreateAsync(CreateCourseProposalDto input);
    Task<CourseProposalDto> ReviewAsync(Guid id, ReviewCourseProposalDto input);
}
