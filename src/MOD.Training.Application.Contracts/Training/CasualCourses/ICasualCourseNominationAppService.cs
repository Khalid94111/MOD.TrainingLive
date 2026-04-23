using MOD.Training.Training.CasualCourses.Dtos;
using MOD.Training.Training.Plans.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.CasualCourses;

public interface ICasualCourseNominationAppService : IApplicationService
{
    Task<List<CasualCourseNominationDto>> GetListByCasualCourseAsync(Guid casualCourseId);

    Task<CasualCourseNominationDto> AddAsync(Guid casualCourseId, Guid employeeId);

    Task RemoveAsync(Guid id);

    Task<CasualCourseNominationDto> ReturnAsync(Guid id, ReturnReasonDto input);

    Task<CasualCourseNominationDto> ReplaceAsync(Guid id, Guid newEmployeeId);
}
