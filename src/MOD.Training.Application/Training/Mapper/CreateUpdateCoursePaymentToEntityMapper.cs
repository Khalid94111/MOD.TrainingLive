using MOD.Training.Training.Payments;
using MOD.Training.Training.Payments.Dtos;
using Riok.Mapperly.Abstractions;
using System;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CreateUpdateCoursePaymentToEntityMapper
    : MapperBase<CreateUpdateCoursePaymentDto, CoursePayment>
{
    public override CoursePayment Map(CreateUpdateCoursePaymentDto source)
    {
        var entity = new CoursePayment(Guid.NewGuid());
        Map(source, entity);
        return entity;
    }

    [MapperIgnoreTarget(nameof(CoursePayment.Id))]
    [MapperIgnoreTarget(nameof(CoursePayment.TenantId))]
    [MapperIgnoreTarget(nameof(CoursePayment.InvoiceBlobName))]
    [MapperIgnoreTarget(nameof(CoursePayment.InvoiceOriginalFileName))]
    [MapperIgnoreTarget(nameof(CoursePayment.Status))]
    [MapperIgnoreTarget(nameof(CoursePayment.ConfirmedAt))]
    [MapperIgnoreTarget(nameof(CoursePayment.ConfirmedById))]
    [MapperIgnoreTarget(nameof(CoursePayment.ExtraProperties))]
    [MapperIgnoreTarget(nameof(CoursePayment.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(CoursePayment.CreationTime))]
    [MapperIgnoreTarget(nameof(CoursePayment.CreatorId))]
    [MapperIgnoreTarget(nameof(CoursePayment.LastModificationTime))]
    [MapperIgnoreTarget(nameof(CoursePayment.LastModifierId))]
    [MapperIgnoreTarget(nameof(CoursePayment.IsDeleted))]
    [MapperIgnoreTarget(nameof(CoursePayment.DeleterId))]
    [MapperIgnoreTarget(nameof(CoursePayment.DeletionTime))]
    public override partial void Map(CreateUpdateCoursePaymentDto source, CoursePayment destination);
}
