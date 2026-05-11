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
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Finance;

[Authorize(TrainingPermissions.TrainingProvider.Default)]
public class TrainingProviderAppService(
    IRepository<TrainingProvider, Guid> repository,
    IRepository<GeographicalLocation, Guid> geoRepository,
    TrainingProviderToDtoMapper toDtoMapper,
    CreateUpdateTrainingProviderToEntityMapper toEntityMapper)
    : CrudAppService<
        TrainingProvider,
        TrainingProviderDto,
        Guid,
        TrainingProviderGetListInput,
        CreateUpdateTrainingProviderDto>(repository),
    ITrainingProviderAppService
{
    protected override async Task<IQueryable<TrainingProvider>> CreateFilteredQueryAsync(
        TrainingProviderGetListInput input)
    {
        var queryable = await base.CreateFilteredQueryAsync(input);

        if (!string.IsNullOrWhiteSpace(input.Filter))
            queryable = queryable.Where(x =>
                x.ProviderNameAr.Contains(input.Filter) ||
                x.ProviderNameEn.Contains(input.Filter));

        if (input.IsActive.HasValue)
            queryable = queryable.Where(x => x.IsActive == input.IsActive.Value);

        if (input.IsApproved.HasValue)
            queryable = queryable.Where(x => x.IsApproved == input.IsApproved.Value);

        // Phase 4B-α filters
        if (input.Scope.HasValue)
            queryable = queryable.Where(x => x.Scope == input.Scope.Value);

        if (input.CountryId.HasValue)
            queryable = queryable.Where(x => x.CountryId == input.CountryId.Value);

        if (input.IsFromNebras.HasValue)
            queryable = queryable.Where(x => x.IsFromNebras == input.IsFromNebras.Value);

        return queryable;
    }

    public override async Task<TrainingProviderDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await EnrichAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<TrainingProviderDto>> GetListAsync(
        TrainingProviderGetListInput input)
    {
        var result = await base.GetListAsync(input);
        await EnrichAsync(result.Items);
        return result;
    }

    public override async Task<TrainingProviderDto> CreateAsync(CreateUpdateTrainingProviderDto input)
    {
        ValidateScopeCountry(input);
        var dto = await base.CreateAsync(input);
        await EnrichAsync(new[] { dto });
        return dto;
    }

    public override async Task<TrainingProviderDto> UpdateAsync(Guid id, CreateUpdateTrainingProviderDto input)
    {
        ValidateScopeCountry(input);
        var dto = await base.UpdateAsync(id, input);
        await EnrichAsync(new[] { dto });
        return dto;
    }

    protected override TrainingProviderDto MapToGetOutputDto(TrainingProvider entity)
        => toDtoMapper.Map(entity);

    protected override TrainingProviderDto MapToGetListOutputDto(TrainingProvider entity)
        => toDtoMapper.Map(entity);

    protected override TrainingProvider MapToEntity(CreateUpdateTrainingProviderDto createInput)
        => toEntityMapper.Map(createInput);

    protected override void MapToEntity(CreateUpdateTrainingProviderDto updateInput, TrainingProvider entity)
    {
        // Preserve sync-driven Nebras fields — never accept them from input even if a client crafts them.
        var preservedIsFromNebras = entity.IsFromNebras;
        var preservedNebrasId = entity.NebrasId;

        toEntityMapper.Map(updateInput, entity);

        entity.IsFromNebras = preservedIsFromNebras;
        entity.NebrasId = preservedNebrasId;
    }

    public async Task<List<TrainingProviderDto>> GetAllActiveAsync()
    {
        var queryable = await Repository.GetQueryableAsync();
        var entities = await AsyncExecuter.ToListAsync(
            queryable.Where(x => x.IsActive).OrderBy(x => x.ProviderNameAr));
        var dtos = entities.Select(toDtoMapper.Map).ToList();
        await EnrichAsync(dtos);
        return dtos;
    }

    private static void ValidateScopeCountry(CreateUpdateTrainingProviderDto input)
    {
        if (input.Scope != ProviderScope.Internal && !input.CountryId.HasValue)
            throw new BusinessException("Training:TrainingProvider:CountryRequiredForExternal");
    }

    private async Task EnrichAsync(IReadOnlyList<TrainingProviderDto> dtos)
    {
        var countryIds = dtos.Where(x => x.CountryId.HasValue)
                             .Select(x => x.CountryId!.Value)
                             .Distinct()
                             .ToList();
        if (countryIds.Count == 0) return;

        var geoQ = await geoRepository.GetQueryableAsync();
        var countries = await AsyncExecuter.ToListAsync(
            geoQ.Where(x => countryIds.Contains(x.Id)));
        var lookup = countries.ToDictionary(x => x.Id);

        foreach (var dto in dtos)
        {
            if (dto.CountryId.HasValue && lookup.TryGetValue(dto.CountryId.Value, out var c))
            {
                dto.CountryNameAr = c.ArabicName;
                dto.CountryNameEn = c.EnglishName;
            }
        }
    }
}
