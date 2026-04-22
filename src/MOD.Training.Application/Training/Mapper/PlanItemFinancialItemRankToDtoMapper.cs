using MOD.Training.Training.Finance;
using MOD.Training.Training.Plans.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class PlanItemFinancialItemRankToDtoMapper : MapperBase<PlanItemFinancialItemRank, PlanItemFinancialItemRankDto>
{
    [MapperIgnoreTarget(nameof(PlanItemFinancialItemRankDto.RankNameAr))]
    [MapperIgnoreTarget(nameof(PlanItemFinancialItemRankDto.RankNameEn))]
    public override partial PlanItemFinancialItemRankDto Map(PlanItemFinancialItemRank source);
    public override partial void Map(PlanItemFinancialItemRank source, PlanItemFinancialItemRankDto destination);
}
