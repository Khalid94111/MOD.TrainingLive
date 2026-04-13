using MOD.Training.Training.Finance;
using MOD.Training.Training.Plans.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class PlanItemFinancialItemToDtoMapper : MapperBase<PlanItemFinancialItem, PlanItemFinancialItemDto>
{
    public override partial PlanItemFinancialItemDto Map(PlanItemFinancialItem source);
    public override partial void Map(PlanItemFinancialItem source, PlanItemFinancialItemDto destination);
}
