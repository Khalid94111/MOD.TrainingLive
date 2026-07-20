using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance.Dtos;
using MOD.Training.Training.Hr;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Finance;

[Authorize(TrainingPermissions.PriceQuote.Default)]
public class PriceQuoteAppService(
    IRepository<PriceQuote, Guid> repository,
    IRepository<TrainingProvider, Guid> providerRepository,
    IRepository<CourseSession, Guid> sessionRepository,
    IRepository<GeographicalLocation, Guid> geoRepository,
    PriceQuoteToDtoMapper toDtoMapper,
    CreateUpdatePriceQuoteToEntityMapper toEntityMapper)
    : ApplicationService, IPriceQuoteAppService
{
    public async Task<PriceQuoteDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        var dto = toDtoMapper.Map(entity);
        await EnrichAsync(new[] { dto }, new[] { entity });
        return dto;
    }

    public async Task<PagedResultDto<PriceQuoteDto>> GetListAsync(PriceQuoteGetListInput input)
    {
        var queryable = await repository.GetQueryableAsync();

        if (input.SessionId.HasValue)
            queryable = queryable.Where(x => x.SessionId == input.SessionId.Value);
        if (input.CasualCourseId.HasValue)
            queryable = queryable.Where(x => x.CasualCourseId == input.CasualCourseId.Value);
        if (input.ProviderId.HasValue)
            queryable = queryable.Where(x => x.ProviderId == input.ProviderId.Value);
        if (input.Status.HasValue)
            queryable = queryable.Where(x => x.Status == input.Status.Value);
        if (input.IsSelected.HasValue)
            queryable = queryable.Where(x => x.IsSelected == input.IsSelected.Value);

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        queryable = queryable.OrderByDescending(x => x.CreationTime).PageBy(input);
        var entities = await AsyncExecuter.ToListAsync(queryable);

        var dtos = entities.Select(toDtoMapper.Map).ToList();
        await EnrichAsync(dtos, entities);
        return new PagedResultDto<PriceQuoteDto>(totalCount, dtos);
    }

    [Authorize(TrainingExecutionPermissions.PriceQuotes.Create)]
    public async Task<PriceQuoteDto> CreateAsync(CreateUpdatePriceQuoteDto input)
    {
        ValidatePolymorphicParent(input);
        await EnsureSessionQuotesOpenAsync(input.SessionId);
        await ValidateCityInCountryAsync(input.CountryId, input.CityId);

        var entity = toEntityMapper.Map(input);

        await repository.InsertAsync(entity, autoSave: true);

        var dto = toDtoMapper.Map(entity);
        await EnrichAsync(new[] { dto }, new[] { entity });
        return dto;
    }

    [Authorize(TrainingExecutionPermissions.PriceQuotes.Edit)]
    public async Task<PriceQuoteDto> UpdateAsync(Guid id, CreateUpdatePriceQuoteDto input)
    {
        ValidatePolymorphicParent(input);
        var entity = await repository.GetAsync(id);
        await EnsureSessionQuotesOpenAsync(entity.SessionId);
        if (entity.IsSelected)
            throw new BusinessException("Training:PriceQuote:CannotEditSelected");
        await ValidateCityInCountryAsync(input.CountryId, input.CityId);

        // Preserve immutable / server-controlled fields.
        var sessionIdOriginal = entity.SessionId;
        var casualOriginal = entity.CasualCourseId;
        var isSelectedOriginal = entity.IsSelected;
        var statusOriginal = entity.Status;

        toEntityMapper.Map(input, entity);

        // Polymorphic parent is set on create only — never re-routed by an update.
        entity.SessionId = sessionIdOriginal;
        entity.CasualCourseId = casualOriginal;
        entity.IsSelected = isSelectedOriginal;
        entity.Status = statusOriginal;

        if (entity.QuotedPriceOMR <= 0 && entity.QuotedPrice > 0)
            entity.QuotedPriceOMR = entity.QuotedPrice;
        if (entity.SessionId.HasValue)
            entity.CalculatePrices();

        await repository.UpdateAsync(entity, autoSave: true);

        var dto = toDtoMapper.Map(entity);
        await EnrichAsync(new[] { dto }, new[] { entity });
        return dto;
    }

    [Authorize(TrainingExecutionPermissions.PriceQuotes.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        await EnsureSessionQuotesOpenAsync(entity.SessionId);
        if (entity.IsSelected)
            throw new BusinessException("Training:PriceQuote:CannotEditSelected");

        await repository.DeleteAsync(id);
    }

    [Authorize(TrainingExecutionPermissions.PriceQuotes.Approve)]
    public async Task ApproveAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        await EnsureSessionQuotesOpenAsync(entity.SessionId);
        entity.Status = ApprovalStatus.Approved;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    [Authorize(TrainingExecutionPermissions.PriceQuotes.Approve)]
    public async Task RejectAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        await EnsureSessionQuotesOpenAsync(entity.SessionId);
        entity.Status = ApprovalStatus.Rejected;
        await repository.UpdateAsync(entity, autoSave: true);
    }

    // ─── Validation helpers ─────────────────────────────────────────────

    private static void ValidatePolymorphicParent(CreateUpdatePriceQuoteDto input)
    {
        var hasSession = input.SessionId.HasValue;
        var hasCasual = input.CasualCourseId.HasValue;
        if (hasSession == hasCasual)
            throw new BusinessException("Training:PriceQuote:OnePolymorphicParentRequired");
    }

    private async Task EnsureSessionQuotesOpenAsync(Guid? sessionId)
    {
        if (!sessionId.HasValue) return;

        var session = await sessionRepository.GetAsync(sessionId.Value);
        if (session.SelectedPriceQuoteId.HasValue)
            throw new BusinessException("Training:PriceQuote:SessionQuotesLocked");
    }

    private async Task ValidateCityInCountryAsync(Guid? countryId, Guid? cityId)
    {
        if (!cityId.HasValue) return;
        if (!countryId.HasValue)
            throw new BusinessException("Training:PriceQuote:CityNotInCountry");

        var city = await geoRepository.FindAsync(cityId.Value);
        if (city == null || city.LocationParentId != countryId.Value)
            throw new BusinessException("Training:PriceQuote:CityNotInCountry");
    }

    // ─── Enrichment (joined fields) ─────────────────────────────────────

    private async Task EnrichAsync(IReadOnlyList<PriceQuoteDto> dtos, IReadOnlyList<PriceQuote> entities)
    {
        if (dtos.Count == 0) return;

        var providerIds = entities.Select(x => x.ProviderId).Distinct().ToList();
        var sessionIds = entities.Where(x => x.SessionId.HasValue)
                                 .Select(x => x.SessionId!.Value).Distinct().ToList();
        var geoIds = entities.SelectMany(x => new[] { x.CountryId, x.CityId })
                             .Where(x => x.HasValue).Select(x => x!.Value)
                             .Distinct().ToList();

        var providerLookup = await BatchFindAsync(providerRepository, providerIds);
        var sessionLookup = await BatchFindAsync(sessionRepository, sessionIds);
        var geoLookup = await BatchFindAsync(geoRepository, geoIds);

        for (var i = 0; i < dtos.Count; i++)
        {
            var dto = dtos[i];
            var ent = entities[i];

            if (providerLookup.TryGetValue(ent.ProviderId, out var prov))
            {
                dto.ProviderName = prov.ProviderNameAr;
                dto.ProviderScope = prov.Scope;
            }

            // Phase 4C-α (v4.10.0): CourseSession no longer carries a SessionCode column.
            // Step 5 of 4C-α will project a session display label via TenantCourse join.
            if (ent.SessionId.HasValue && sessionLookup.TryGetValue(ent.SessionId.Value, out _))
            {
                dto.SessionCode = string.Empty;
            }

            if (ent.CountryId.HasValue && geoLookup.TryGetValue(ent.CountryId.Value, out var country))
            {
                dto.CountryNameAr = country.ArabicName;
                dto.CountryNameEn = country.EnglishName;
            }

            if (ent.CityId.HasValue && geoLookup.TryGetValue(ent.CityId.Value, out var city))
            {
                dto.CityNameAr = city.ArabicName;
                dto.CityNameEn = city.EnglishName;
            }
        }
    }

    private async Task<Dictionary<Guid, T>> BatchFindAsync<T>(IRepository<T, Guid> repo, List<Guid> ids)
        where T : class, Volo.Abp.Domain.Entities.IEntity<Guid>
    {
        if (ids.Count == 0) return new Dictionary<Guid, T>();
        var queryable = await repo.GetQueryableAsync();
        var rows = await AsyncExecuter.ToListAsync(queryable.Where(x => ids.Contains(x.Id)));
        return rows.ToDictionary(x => x.Id);
    }
}
