using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.CasualCourses.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CasualCourseFinancialToDtoMapper : MapperBase<CasualCourseFinancial, CasualCourseFinancialDto>
{
    [MapperIgnoreTarget(nameof(CasualCourseFinancialDto.FinancialItemName))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancialDto.IsPerDay))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancialDto.IsPerNominee))]
    public override partial CasualCourseFinancialDto Map(CasualCourseFinancial source);
    public override partial void Map(CasualCourseFinancial source, CasualCourseFinancialDto destination);
}
