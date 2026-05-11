using MOD.Training.Training.Payments;
using MOD.Training.Training.Payments.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class BudgetReallocationToDtoMapper : MapperBase<BudgetReallocation, BudgetReallocationDto>
{
    [MapperIgnoreTarget(nameof(BudgetReallocationDto.CasualCourseNameAr))]
    [MapperIgnoreTarget(nameof(BudgetReallocationDto.FundingScenario))]
    [MapperIgnoreTarget(nameof(BudgetReallocationDto.FundingSourceName))]
    [MapperIgnoreTarget(nameof(BudgetReallocationDto.ToFinancialItemNameAr))]
    [MapperIgnoreTarget(nameof(BudgetReallocationDto.ApprovedByName))]
    public override partial BudgetReallocationDto Map(BudgetReallocation source);
    public override partial void Map(BudgetReallocation source, BudgetReallocationDto destination);
}
