using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Finance.Dtos;
using MOD.Training.Training.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Finance;

[Authorize(TrainingPermissions.ExchangeRates.Default)]
public class ExchangeRateAppService(
    IRepository<ExchangeRate, Guid> exchangeRateRepo,
    ExchangeRateToDtoMapper toDtoMapper)
    : ApplicationService, IExchangeRateAppService
{
    public async Task<PagedResultDto<ExchangeRateDto>> GetListAsync(ExchangeRateGetListInput input)
    {
        var queryable = await exchangeRateRepo.GetQueryableAsync();
        var totalCount = await AsyncExecuter.CountAsync(queryable);

        queryable = queryable.OrderByDescending(x => x.SetAt);

        if (input.SkipCount > 0)
            queryable = queryable.Skip(input.SkipCount);
        if (input.MaxResultCount > 0)
            queryable = queryable.Take(input.MaxResultCount);

        var entities = await AsyncExecuter.ToListAsync(queryable);
        var dtos = entities.Select(toDtoMapper.Map).ToList();

        return new PagedResultDto<ExchangeRateDto>(totalCount, dtos);
    }

    public async Task<ExchangeRateDto> GetActiveAsync()
    {
        var queryable = await exchangeRateRepo.GetQueryableAsync();
        var active = await AsyncExecuter.FirstOrDefaultAsync(
            queryable.Where(x => x.IsActive))
            ?? throw new BusinessException("Training:ExchangeRate:NoActive");

        return toDtoMapper.Map(active);
    }

    [Authorize(TrainingPermissions.ExchangeRates.Create)]
    public async Task<ExchangeRateDto> CreateAsync(CreateExchangeRateDto input)
    {
        // Deactivate all current active rates
        var queryable = await exchangeRateRepo.GetQueryableAsync();
        var activeRates = await AsyncExecuter.ToListAsync(
            queryable.Where(x => x.IsActive));

        foreach (var rate in activeRates)
        {
            rate.IsActive = false;
            await exchangeRateRepo.UpdateAsync(rate);
        }

        // Create new active rate
        var entity = new ExchangeRate
        {
            FromCurrency = "USD",
            ToCurrency = "OMR",
            Rate = input.Rate,
            IsActive = true,
            SetById = CurrentUser.Id!.Value,
            SetAt = DateTime.UtcNow
        };

        await exchangeRateRepo.InsertAsync(entity);
        return toDtoMapper.Map(entity);
    }

    [Authorize(TrainingPermissions.ExchangeRates.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var entity = await exchangeRateRepo.GetAsync(id);

        if (entity.IsActive)
        {
            throw new BusinessException("Training:ExchangeRate:CannotDeleteActive");
        }

        await exchangeRateRepo.DeleteAsync(id);
    }
}
