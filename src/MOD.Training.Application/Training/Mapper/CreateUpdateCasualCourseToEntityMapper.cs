using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.CasualCourses.Dtos;
using Riok.Mapperly.Abstractions;
using System;
using Volo.Abp.Mapperly;
using Volo.Abp.Users;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CreateUpdateCasualCourseToEntityMapper(ICurrentUser currentUser)
    : MapperBase<CreateUpdateCasualCourseDto, CasualCourse>
{
    public override CasualCourse Map(CreateUpdateCasualCourseDto source)
    {
        var entity = new CasualCourse(
            Guid.NewGuid(),
            source.TenantCourseId,
            source.UnitId,
            currentUser.Id!.Value,
            source.CourseType,
            source.Priority,
            source.Justification,
            source.DurationDays,
            source.EstimatedDateFrom,
            source.EstimatedDateTo);
        Map(source, entity);
        return entity;
    }

    [MapperIgnoreTarget(nameof(CasualCourse.Id))]
    [MapperIgnoreTarget(nameof(CasualCourse.TenantId))]
    [MapperIgnoreTarget(nameof(CasualCourse.RequestedById))]
    [MapperIgnoreTarget(nameof(CasualCourse.Status))]
    [MapperIgnoreTarget(nameof(CasualCourse.ReturnedFromStatus))]
    [MapperIgnoreTarget(nameof(CasualCourse.IsReturned))]
    [MapperIgnoreTarget(nameof(CasualCourse.LastReturnNoteId))]
    [MapperIgnoreTarget(nameof(CasualCourse.EstimatedTotalCost))]
    [MapperIgnoreTarget(nameof(CasualCourse.FundingScenario))]
    [MapperIgnoreTarget(nameof(CasualCourse.SelectedPriceQuoteId))]
    [MapperIgnoreTarget(nameof(CasualCourse.RejectedReason))]
    [MapperIgnoreTarget(nameof(CasualCourse.FinancialItems))]
    [MapperIgnoreTarget(nameof(CasualCourse.Nominations))]
    [MapperIgnoreTarget(nameof(CasualCourse.CreationTime))]
    [MapperIgnoreTarget(nameof(CasualCourse.CreatorId))]
    [MapperIgnoreTarget(nameof(CasualCourse.LastModificationTime))]
    [MapperIgnoreTarget(nameof(CasualCourse.LastModifierId))]
    [MapperIgnoreTarget(nameof(CasualCourse.IsDeleted))]
    [MapperIgnoreTarget(nameof(CasualCourse.DeleterId))]
    [MapperIgnoreTarget(nameof(CasualCourse.DeletionTime))]
    public override partial void Map(CreateUpdateCasualCourseDto source, CasualCourse destination);
}
