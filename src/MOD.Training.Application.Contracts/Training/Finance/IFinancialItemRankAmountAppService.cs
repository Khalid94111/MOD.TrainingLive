using MOD.Training.Training.Finance.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Finance;

public interface IFinancialItemRankAmountAppService : IApplicationService
{
    Task<FinancialItemRankAmountDto> GetAsync(Guid id);
    Task<List<FinancialItemRankAmountDto>> GetByFinancialItemAsync(Guid financialItemId);
    Task<FinancialItemRankAmountDto> CreateAsync(CreateUpdateFinancialItemRankAmountDto input);
    Task<FinancialItemRankAmountDto> UpdateAsync(Guid id, CreateUpdateFinancialItemRankAmountDto input);
    Task DeleteAsync(Guid id);
}
