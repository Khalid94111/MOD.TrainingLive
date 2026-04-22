using MOD.Training.Training.Finance;
using MOD.Training.Training.Finance.Dtos;
using Riok.Mapperly.Abstractions;
using System;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class FinancialItemRankAmountToDtoMapper : MapperBase<FinancialItemRankAmount, FinancialItemRankAmountDto>
{
    [MapperIgnoreTarget(nameof(FinancialItemRankAmountDto.RankNameAr))]
    [MapperIgnoreTarget(nameof(FinancialItemRankAmountDto.RankNameEn))]
    public override partial FinancialItemRankAmountDto Map(FinancialItemRankAmount source);
    public override partial void Map(FinancialItemRankAmount source, FinancialItemRankAmountDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class CreateUpdateFinancialItemRankAmountToEntityMapper : MapperBase<CreateUpdateFinancialItemRankAmountDto, FinancialItemRankAmount>
{
    public override FinancialItemRankAmount Map(CreateUpdateFinancialItemRankAmountDto source)
    {
        var entity = new FinancialItemRankAmount(Guid.NewGuid(), source.FinancialItemId, source.RankId, source.AmountOMR);
        return entity;
    }

    public override void Map(CreateUpdateFinancialItemRankAmountDto source, FinancialItemRankAmount destination)
    {
        destination.FinancialItemId = source.FinancialItemId;
        destination.RankId = source.RankId;
        destination.AmountOMR = source.AmountOMR;
    }
}
