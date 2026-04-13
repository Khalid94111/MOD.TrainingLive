using MOD.Training.Training.Finance;
using MOD.Training.Training.Finance.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class PriceQuoteToDtoMapper : MapperBase<PriceQuote, PriceQuoteDto>
{
    public override partial PriceQuoteDto Map(PriceQuote source);
    public override partial void Map(PriceQuote source, PriceQuoteDto destination);
}
