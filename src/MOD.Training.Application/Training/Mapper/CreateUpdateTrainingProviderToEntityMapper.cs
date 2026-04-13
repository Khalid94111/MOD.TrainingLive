using System;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Finance.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CreateUpdateTrainingProviderToEntityMapper : MapperBase<CreateUpdateTrainingProviderDto, TrainingProvider>
{
    public override TrainingProvider Map(CreateUpdateTrainingProviderDto source)
    {
        var entity = new TrainingProvider(Guid.NewGuid(), source.ProviderNameAr, source.ProviderNameEn);
        Map(source, entity);
        return entity;
    }

    public override partial void Map(CreateUpdateTrainingProviderDto source, TrainingProvider destination);
}
