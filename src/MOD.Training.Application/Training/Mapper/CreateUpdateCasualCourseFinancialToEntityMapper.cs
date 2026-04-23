using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.CasualCourses.Dtos;
using MOD.Training.Training.Enums;
using Riok.Mapperly.Abstractions;
using System;
using Volo.Abp.Mapperly;

namespace MOD.Training.Training;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CreateUpdateCasualCourseFinancialToEntityMapper
    : MapperBase<CreateCasualCourseFinancialDto, CasualCourseFinancial>
{
    public override CasualCourseFinancial Map(CreateCasualCourseFinancialDto source)
    {
        var entity = new CasualCourseFinancial(
            Guid.NewGuid(),
            Guid.Empty, // set by AppService
            source.FinancialItemId,
            source.EstimatedAmountOMR,
            FinancialAmountSource.FundingSource); // default — AppService may override
        Map(source, entity);
        return entity;
    }

    [MapperIgnoreTarget(nameof(CasualCourseFinancial.Id))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancial.TenantId))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancial.CasualCourseId))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancial.Source))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancial.ActualAmountOMR))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancial.CreationTime))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancial.CreatorId))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancial.LastModificationTime))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancial.LastModifierId))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancial.IsDeleted))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancial.DeleterId))]
    [MapperIgnoreTarget(nameof(CasualCourseFinancial.DeletionTime))]
    public override partial void Map(CreateCasualCourseFinancialDto source, CasualCourseFinancial destination);
}
