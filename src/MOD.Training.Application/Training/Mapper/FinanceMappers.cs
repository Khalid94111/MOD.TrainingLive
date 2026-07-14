using MOD.Training.Training.Finance.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training.Finance;

// ============================================================
// FinancialItem Mappers
// ============================================================

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class FinancialItemToDtoMapper : MapperBase<FinancialItem, FinancialItemDto>
{
    public override partial FinancialItemDto Map(FinancialItem source);
    public override partial void Map(FinancialItem source, FinancialItemDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CreateUpdateFinancialItemToEntityMapper : MapperBase<CreateUpdateFinancialItemDto, FinancialItem>
{
    public override partial FinancialItem Map(CreateUpdateFinancialItemDto source);
    public override partial void Map(CreateUpdateFinancialItemDto source, FinancialItem destination);
}

// ============================================================
// TrainingBudget Mappers
// ============================================================

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TrainingBudgetToDtoMapper : MapperBase<TrainingBudget, TrainingBudgetDto>
{
    public override partial TrainingBudgetDto Map(TrainingBudget source);
    public override partial void Map(TrainingBudget source, TrainingBudgetDto destination);
}
