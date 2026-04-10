using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Finance;

public interface ICourseTypeFinancialDefaultAppService : IApplicationService
{
    Task<List<CourseTypeFinancialItemDefaultDto>> GetListAsync(CourseType courseType);
    Task<CourseTypeFinancialItemDefaultDto> CreateAsync(CreateCourseTypeFinancialItemDefaultDto input);
    Task DeleteAsync(Guid id);
}
