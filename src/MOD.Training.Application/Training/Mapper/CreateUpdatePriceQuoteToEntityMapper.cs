using MOD.Training.Training.Finance;
using MOD.Training.Training.Finance.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;
using System;
namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CreateUpdatePriceQuoteToEntityMapper : MapperBase<CreateUpdatePriceQuoteDto, PriceQuote>
{
    public override PriceQuote Map(CreateUpdatePriceQuoteDto source)
    {
        var entity = new PriceQuote(Guid.NewGuid(), source.SessionId, source.ProviderId, source.PricingType, source.QuotedPrice, source.ParticipantsCount);
        Map(source, entity);
        return entity;
    }
    public override partial void Map(CreateUpdatePriceQuoteDto source, PriceQuote destination);
}
