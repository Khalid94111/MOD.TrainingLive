using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training;
using MOD.Training.Training.Finance.Dtos;
using MOD.Training.Training.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
 
namespace MOD.Training.Training.Finance;

[Authorize(TrainingPermissions.TrainingProvider.Default)]
public class TrainingProviderAppService(
    IRepository<TrainingProvider, Guid> repository,
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

        return queryable;
    }

    protected override TrainingProviderDto MapToGetOutputDto(TrainingProvider entity)
        => toDtoMapper.Map(entity);

    protected override TrainingProviderDto MapToGetListOutputDto(TrainingProvider entity)
        => toDtoMapper.Map(entity);

    protected override TrainingProvider MapToEntity(CreateUpdateTrainingProviderDto createInput)
        => toEntityMapper.Map(createInput);

    protected override void MapToEntity(CreateUpdateTrainingProviderDto updateInput, TrainingProvider entity)
        => toEntityMapper.Map(updateInput, entity);

    public async Task<List<TrainingProviderDto>> GetAllActiveAsync()
    {
        var queryable = await Repository.GetQueryableAsync();
        var entities = await AsyncExecuter.ToListAsync(
            queryable.Where(x => x.IsActive).OrderBy(x => x.ProviderNameAr));
        return entities.Select(e => toDtoMapper.Map(e)).ToList();
    }
}
