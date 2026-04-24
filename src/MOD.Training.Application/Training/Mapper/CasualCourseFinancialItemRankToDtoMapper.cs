using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.CasualCourses.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CasualCourseFinancialItemRankToDtoMapper
    : MapperBase<CasualCourseFinancialItemRank, CasualCourseFinancialItemRankDto>
{
    [MapperIgnoreTarget(nameof(CasualCourseFinancialItemRankDto.RankNameAr))]
    public override partial CasualCourseFinancialItemRankDto Map(CasualCourseFinancialItemRank source);
    public override partial void Map(CasualCourseFinancialItemRank source, CasualCourseFinancialItemRankDto destination);
}
