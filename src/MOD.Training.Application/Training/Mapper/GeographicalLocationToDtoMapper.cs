using MOD.Training.Training.Hr;
using MOD.Training.Training.HrIntegration.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class GeographicalLocationToDtoMapper : MapperBase<GeographicalLocation, GeographicalLocationDto>
{
    public override partial GeographicalLocationDto Map(GeographicalLocation source);
    public override partial void Map(GeographicalLocation source, GeographicalLocationDto destination);
}
