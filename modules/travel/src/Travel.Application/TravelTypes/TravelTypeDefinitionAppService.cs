using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Travel.Localization;
using Travel.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Travel.TravelTypes;

[Authorize(TravelManagementPermissions.TravelTypes.Default)]
public class TravelTypeDefinitionAppService :
    CrudAppService<
        TravelTypeDefinition,
        TravelTypeDefinitionDto,
        Guid,
        GetTravelTypeDefinitionListInput,
        CreateUpdateTravelTypeDefinitionDto>,
    ITravelTypeDefinitionAppService
{
    public TravelTypeDefinitionAppService(IRepository<TravelTypeDefinition, Guid> repository) : base(repository)
    {
        ObjectMapperContext = typeof(TravelApplicationModule);
        LocalizationResource = typeof(TravelResource);
        GetPolicyName = TravelManagementPermissions.TravelTypes.Default;
        GetListPolicyName = TravelManagementPermissions.TravelTypes.Default;
        CreatePolicyName = TravelManagementPermissions.TravelTypes.Create;
        UpdatePolicyName = TravelManagementPermissions.TravelTypes.Edit;
        DeletePolicyName = TravelManagementPermissions.TravelTypes.Delete;
    }

    protected override async Task<IQueryable<TravelTypeDefinition>> CreateFilteredQueryAsync(GetTravelTypeDefinitionListInput input)
    {
        var query = await Repository.GetQueryableAsync();

        return query
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive!.Value)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), x => x.Name.Contains(input.Filter!));
    }

    protected override Task<TravelTypeDefinition> MapToEntityAsync(CreateUpdateTravelTypeDefinitionDto createInput)
    {
        return Task.FromResult(new TravelTypeDefinition(
            Guid.NewGuid(),
            createInput.Code,
            createInput.Name,
            createInput.IsActive));
    }

    protected override Task MapToEntityAsync(CreateUpdateTravelTypeDefinitionDto updateInput, TravelTypeDefinition entity)
    {
        entity.Update(updateInput.Code, updateInput.Name, updateInput.IsActive);
        return Task.CompletedTask;
    }
}
