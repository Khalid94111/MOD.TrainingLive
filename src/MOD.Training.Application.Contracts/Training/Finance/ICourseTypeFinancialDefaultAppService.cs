using MOD.Training.Shared;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Finance;

public interface ICourseTypeFinancialDefaultAppService : IApplicationService
{
    Task<ListResultDto<CourseTypeFinancialItemDefaultDto>> GetListAsync(CourseType courseType);
    Task<CourseTypeFinancialItemDefaultDto> CreateAsync(CreateCourseTypeFinancialItemDefaultDto input);
    Task DeleteAsync(Guid id);
    Task UpdateSortOrderAsync(UpdateSortOrderInput input);

}
