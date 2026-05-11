using MOD.Training.Training.Payments;
using MOD.Training.Training.Payments.Dtos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CoursePaymentToDtoMapper : MapperBase<CoursePayment, CoursePaymentDto>
{
    [MapperIgnoreTarget(nameof(CoursePaymentDto.TrainingProviderName))]
    [MapperIgnoreTarget(nameof(CoursePaymentDto.CourseNameAr))]
    [MapperIgnoreTarget(nameof(CoursePaymentDto.FundingScenario))]
    [MapperIgnoreTarget(nameof(CoursePaymentDto.HasInvoice))]
    [MapperIgnoreTarget(nameof(CoursePaymentDto.ConfirmedByName))]
    public override partial CoursePaymentDto Map(CoursePayment source);
    public override partial void Map(CoursePayment source, CoursePaymentDto destination);
}
