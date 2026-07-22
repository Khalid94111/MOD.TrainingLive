using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Travel.Allowances;

public interface IAllowanceRuleAppService : ICrudAppService<
    AllowanceRuleDto,
    Guid,
    GetAllowanceRuleListInput,
    CreateUpdateAllowanceRuleDto>
{
    Task<AllowanceRuleTestResultDto> TestAsync(Guid id, AllowanceRuleTestInputDto input);
}
