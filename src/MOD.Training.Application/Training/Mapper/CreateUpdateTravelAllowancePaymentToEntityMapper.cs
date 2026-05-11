using MOD.Training.Training.Payments;
using MOD.Training.Training.Payments.Dtos;
using Riok.Mapperly.Abstractions;
using System;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CreateUpdateTravelAllowancePaymentToEntityMapper
    : MapperBase<CreateUpdateTravelAllowancePaymentDto, TravelAllowancePayment>
{
    public override TravelAllowancePayment Map(CreateUpdateTravelAllowancePaymentDto source)
    {
        var entity = new TravelAllowancePayment(Guid.NewGuid());
        Map(source, entity);
        return entity;
    }

    [MapperIgnoreTarget(nameof(TravelAllowancePayment.Id))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.TenantId))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.PersonnelType))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.TotalOMR))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.Status))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.ConfirmedAt))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.ConfirmedById))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.ExternalRequestId))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.ExternalStatus))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.ExternalResponseAt))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.ExtraProperties))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.CreationTime))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.CreatorId))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.LastModificationTime))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.LastModifierId))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.IsDeleted))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.DeleterId))]
    [MapperIgnoreTarget(nameof(TravelAllowancePayment.DeletionTime))]
    public override partial void Map(CreateUpdateTravelAllowancePaymentDto source, TravelAllowancePayment destination);
}
