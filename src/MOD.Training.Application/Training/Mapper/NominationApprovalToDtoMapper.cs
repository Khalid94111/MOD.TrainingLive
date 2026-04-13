using MOD.Training.Training.Nominations;
using MOD.Training.Training.Nominations.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class NominationApprovalToDtoMapper : MapperBase<NominationApproval, NominationApprovalDto>
{
    public override partial NominationApprovalDto Map(NominationApproval source);
    public override partial void Map(NominationApproval source, NominationApprovalDto destination);
}
