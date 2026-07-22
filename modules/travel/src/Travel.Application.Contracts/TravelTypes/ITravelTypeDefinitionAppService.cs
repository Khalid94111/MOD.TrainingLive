using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Travel.TravelTypes;

public interface ITravelTypeDefinitionAppService : ICrudAppService<
    TravelTypeDefinitionDto,
    Guid,
    GetTravelTypeDefinitionListInput,
    CreateUpdateTravelTypeDefinitionDto>
{
}
