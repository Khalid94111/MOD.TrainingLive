using MOD.Training.Training.CasualCourses.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.CasualCourses;

public interface ICasualCourseFinancialItemAppService : IApplicationService
{
    Task<List<CasualCourseFinancialItemDto>> GetListByCasualCourseAsync(Guid casualCourseId);

    Task<CasualCourseFinancialItemDto> AddItemAsync(Guid casualCourseId, CreateCasualCourseFinancialItemDto input);

    Task DeleteItemAsync(Guid id);
}
