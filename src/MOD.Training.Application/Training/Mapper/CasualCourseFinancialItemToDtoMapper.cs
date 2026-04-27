using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.CasualCourses.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CasualCourseFinancialItemToDtoMapper : MapperBase<CasualCourseFinancialItem, CasualCourseFinancialItemDto>
{
    [MapperIgnoreTarget(nameof(CasualCourseFinancialItemDto.FinancialItemName))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancialItemDto.IsPerDay))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancialItemDto.IsPerNominee))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancialItemDto.ExtraDaysBefore))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancialItemDto.ExtraDaysAfter))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancialItemDto.EffectiveDays))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancialItemDto.Ranks))]
    public override partial CasualCourseFinancialItemDto Map(CasualCourseFinancialItem source);
    public override partial void Map(CasualCourseFinancialItem source, CasualCourseFinancialItemDto destination);
}
