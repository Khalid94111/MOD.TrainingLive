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
// ExchangeRate Mappers
// ============================================================

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class ExchangeRateToDtoMapper : MapperBase<ExchangeRate, ExchangeRateDto>
{
    public override partial ExchangeRateDto Map(ExchangeRate source);
    public override partial void Map(ExchangeRate source, ExchangeRateDto destination);
}

// ============================================================
// TrainingBudget Mappers
// ============================================================

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TrainingBudgetToDtoMapper : MapperBase<TrainingBudget, TrainingBudgetDto>
{
    public override partial TrainingBudgetDto Map(TrainingBudget source);
    public override partial void Map(TrainingBudget source, TrainingBudgetDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CreateUpdateTrainingBudgetToEntityMapper : MapperBase<CreateUpdateTrainingBudgetDto, TrainingBudget>
{
    public override partial TrainingBudget Map(CreateUpdateTrainingBudgetDto source);
    public override partial void Map(CreateUpdateTrainingBudgetDto source, TrainingBudget destination);
}
