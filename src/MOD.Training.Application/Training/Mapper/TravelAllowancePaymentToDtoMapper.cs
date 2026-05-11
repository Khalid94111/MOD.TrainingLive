using MOD.Training.Training.Payments;
using MOD.Training.Training.Payments.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TravelAllowancePaymentToDtoMapper : MapperBase<TravelAllowancePayment, TravelAllowancePaymentDto>
{
    [MapperIgnoreTarget(nameof(TravelAllowancePaymentDto.EmployeeNameAr))]
    [MapperIgnoreTarget(nameof(TravelAllowancePaymentDto.RankNameAr))]
    [MapperIgnoreTarget(nameof(TravelAllowancePaymentDto.CourseNameAr))]
    [MapperIgnoreTarget(nameof(TravelAllowancePaymentDto.EffectiveTravelDays))]
    [MapperIgnoreTarget(nameof(TravelAllowancePaymentDto.ConfirmedByName))]
    public override partial TravelAllowancePaymentDto Map(TravelAllowancePayment source);
    public override partial void Map(TravelAllowancePayment source, TravelAllowancePaymentDto destination);
}
