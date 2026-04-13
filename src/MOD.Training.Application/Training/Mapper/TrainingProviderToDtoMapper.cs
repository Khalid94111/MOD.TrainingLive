using MOD.Training.Training.Finance;
using MOD.Training.Training.Finance.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TrainingProviderToDtoMapper : MapperBase<TrainingProvider, TrainingProviderDto>
{
    public override partial TrainingProviderDto Map(TrainingProvider source);
    public override partial void Map(TrainingProvider source, TrainingProviderDto destination);
}
