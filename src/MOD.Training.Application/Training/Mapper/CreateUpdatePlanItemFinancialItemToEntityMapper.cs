using MOD.Training.Training.Finance;
using MOD.Training.Training.Plans.Dtos;
using Riok.Mapperly.Abstractions;
using System;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CreateUpdatePlanItemFinancialItemToEntityMapper : MapperBase<CreateUpdatePlanItemFinancialItemDto, PlanItemFinancialItem>
{
    public override   PlanItemFinancialItem Map(CreateUpdatePlanItemFinancialItemDto source)
    {
        var entity = new PlanItemFinancialItem(Guid.NewGuid(), source.PlanItemId, source.FinancialItemId, source.EstimatedAmountOMR);
        Map(source, entity);
        return entity;
    }
    public override partial void Map(CreateUpdatePlanItemFinancialItemDto source, PlanItemFinancialItem destination);
}
