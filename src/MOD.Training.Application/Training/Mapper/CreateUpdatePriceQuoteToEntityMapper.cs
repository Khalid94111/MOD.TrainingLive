using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Finance.Dtos;
using Riok.Mapperly.Abstractions;
using System;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CreateUpdatePriceQuoteToEntityMapper : MapperBase<CreateUpdatePriceQuoteDto, PriceQuote>
{
    public override PriceQuote Map(CreateUpdatePriceQuoteDto source)
    {
        var entity = new PriceQuote(Guid.NewGuid());
        Map(source, entity);
        // QuotedPriceOMR fallback: legacy session arm only sets QuotedPrice; mirror it.
        if (entity.QuotedPriceOMR <= 0 && entity.QuotedPrice > 0)
            entity.QuotedPriceOMR = entity.QuotedPrice;
        if (entity.SessionId.HasValue)
            entity.CalculatePrices();
        return entity;
    }

    [MapperIgnoreTarget(nameof(PriceQuote.Id))]
    [MapperIgnoreTarget(nameof(PriceQuote.TenantId))]
    [MapperIgnoreTarget(nameof(PriceQuote.IsSelected))]
    [MapperIgnoreTarget(nameof(PriceQuote.Status))]
    [MapperIgnoreTarget(nameof(PriceQuote.PricePerPerson))]
    [MapperIgnoreTarget(nameof(PriceQuote.TotalPrice))]
    [MapperIgnoreTarget(nameof(PriceQuote.Provider))]
    [MapperIgnoreTarget(nameof(PriceQuote.Session))]
    [MapperIgnoreTarget(nameof(PriceQuote.CreationTime))]
    [MapperIgnoreTarget(nameof(PriceQuote.CreatorId))]
    [MapperIgnoreTarget(nameof(PriceQuote.LastModificationTime))]
    [MapperIgnoreTarget(nameof(PriceQuote.LastModifierId))]
    [MapperIgnoreTarget(nameof(PriceQuote.IsDeleted))]
    [MapperIgnoreTarget(nameof(PriceQuote.DeleterId))]
    [MapperIgnoreTarget(nameof(PriceQuote.DeletionTime))]
    public override partial void Map(CreateUpdatePriceQuoteDto source, PriceQuote destination);
}
