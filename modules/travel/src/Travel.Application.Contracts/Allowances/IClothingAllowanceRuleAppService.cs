using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Travel.Allowances;

public interface IClothingAllowanceRuleAppService : ICrudAppService<
    ClothingAllowanceRuleDto,
    Guid,
    GetClothingAllowanceRuleListInput,
    CreateUpdateClothingAllowanceRuleDto>
{
}
