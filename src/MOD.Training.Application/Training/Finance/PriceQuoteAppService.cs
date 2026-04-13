using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance.Dtos;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
 

namespace MOD.Training.Training.Finance;

[Authorize(TrainingPermissions.PriceQuote.Default)]
public class PriceQuoteAppService(
    IRepository<PriceQuote, Guid> repository,
    IRepository<TrainingProvider, Guid> providerRepository,
    IRepository<CourseSession, Guid> sessionRepository,
    PriceQuoteToDtoMapper toDtoMapper,
    CreateUpdatePriceQuoteToEntityMapper toEntityMapper)
    : ApplicationService, IPriceQuoteAppService
{
    public async Task<PriceQuoteDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        var dto = toDtoMapper.Map(entity);
        await EnrichDtoAsync(dto, entity);
        return dto;
    }

    public async Task<PagedResultDto<PriceQuoteDto>> GetListAsync(PriceQuoteGetListInput input)
    {
        var queryable = await repository.GetQueryableAsync();

        if (input.SessionId.HasValue)
            queryable = queryable.Where(x => x.SessionId == input.SessionId.Value);
        if (input.ProviderId.HasValue)
            queryable = queryable.Where(x => x.ProviderId == input.ProviderId.Value);
        if (input.Status.HasValue)
            queryable = queryable.Where(x => x.Status == input.Status.Value);

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        queryable = queryable.OrderByDescending(x => x.CreationTime);
        queryable = queryable.PageBy(input);

        var entities = await AsyncExecuter.ToListAsync(queryable);

        // Batch load provider names
        var providerIds = entities.Select(x => x.ProviderId).Distinct().ToList();
        var provQueryable = await providerRepository.GetQueryableAsync();
        var providers = await AsyncExecuter.ToListAsync(
            provQueryable.Where(x => providerIds.Contains(x.Id)));
        var provLookup = providers.ToDictionary(x => x.Id);

        var dtos = entities.Select(e =>
        {
            var dto = toDtoMapper.Map(e);
            if (provLookup.TryGetValue(e.ProviderId, out var prov))
                dto.ProviderName = prov.ProviderNameAr;
            return dto;
        }).ToList();

        return new PagedResultDto<PriceQuoteDto>(totalCount, dtos);
    }

    [Authorize(TrainingPermissions.PriceQuote.Create)]
    public async Task<PriceQuoteDto> CreateAsync(CreateUpdatePriceQuoteDto input)
    {
        var entity = new PriceQuote(
            GuidGenerator.Create(),
            input.SessionId,
            input.ProviderId,
            input.PricingType,
            input.QuotedPrice,
            input.ParticipantsCount);
        entity.Notes = input.Notes;

        await repository.InsertAsync(entity, autoSave: true);
        var dto = toDtoMapper.Map(entity);
        await EnrichDtoAsync(dto, entity);
        return dto;
    }

    [Authorize(TrainingPermissions.PriceQuote.Update)]
    public async Task<PriceQuoteDto> UpdateAsync(Guid id, CreateUpdatePriceQuoteDto input)
    {
        var entity = await repository.GetAsync(id);
        entity.PricingType = input.PricingType;
        entity.QuotedPrice = input.QuotedPrice;
        entity.ParticipantsCount = input.ParticipantsCount;
        entity.Notes = input.Notes;
        entity.CalculatePrices();

        await repository.UpdateAsync(entity, autoSave: true);
        return toDtoMapper.Map(entity);
    }

    [Authorize(TrainingPermissions.PriceQuote.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await repository.DeleteAsync(id);
    }

    public async Task ApproveAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        entity.Status = ApprovalStatus.Approved;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    public async Task RejectAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        entity.Status = ApprovalStatus.Rejected;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    private async Task EnrichDtoAsync(PriceQuoteDto dto, PriceQuote entity)
    {
        var provider = await providerRepository.FindAsync(entity.ProviderId);
        if (provider != null)
            dto.ProviderName = provider.ProviderNameAr;

        var session = await sessionRepository.FindAsync(entity.SessionId);
        if (session != null)
            dto.SessionCode = session.SessionCode;
    }
}
