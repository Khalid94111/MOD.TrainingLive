using MOD.Training.Training.CasualCourses.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.CasualCourses;

public interface ICasualCourseFinancialAppService : IApplicationService
{
    Task<List<CasualCourseFinancialDto>> GetListByCasualCourseAsync(Guid casualCourseId);

    Task<List<CasualCourseFinancialDto>> AutoFillFromDefaultsAsync(Guid casualCourseId);

    Task<CasualCourseFinancialDto> AddItemAsync(Guid casualCourseId, CreateCasualCourseFinancialDto input);

    Task<CasualCourseFinancialDto> UpdateAmountAsync(Guid id, UpdateAmountDto input);

    Task DeleteItemAsync(Guid id);
}
