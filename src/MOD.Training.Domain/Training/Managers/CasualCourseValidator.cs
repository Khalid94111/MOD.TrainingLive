using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace MOD.Training.Training.Managers;

public class CasualCourseValidator(
    IRepository<CasualCourse, Guid> casualCourseRepo,
    IRepository<CasualCourseNomination, Guid> nominationRepo)
    : DomainService
{
    public void ValidateCourseType(CourseType courseType)
    {
        if (courseType != CourseType.ExternalLocal
            && courseType != CourseType.ExternalInternational)
        {
            throw new BusinessException("Training:CasualCourse:ExternalCourseTypeRequired");
        }
    }

    public async Task ValidateForSubmitAsync(Guid casualCourseId)
    {
        var cc = await casualCourseRepo.GetAsync(casualCourseId);

        ValidateCourseType(cc.CourseType);

        if (cc.Status != CasualCourseStatus.Draft &&
            cc.Status != CasualCourseStatus.ReturnedToCreator)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        if (cc.DurationDays < 1)
            throw new BusinessException("Training:CasualCourse:DurationRequired");

        if (cc.EstimatedDateFrom > cc.EstimatedDateTo)
            throw new BusinessException("Training:CasualCourse:DateRangeInvalid");

        if (string.IsNullOrWhiteSpace(cc.FundingSourceName))
            throw new BusinessException("Training:CasualCourse:FundingSourceNameRequired");
        if (string.IsNullOrWhiteSpace(cc.FundingSourceVoteCode))
            throw new BusinessException("Training:CasualCourse:FundingSourceVoteCodeRequired");

        // Patch 5 — UTM no longer enters financial rows; Staff creates them after scenario pick
        // during review. Submit no longer requires any CasualCourseFinancialItem rows to exist.

        var nomQ = await nominationRepo.GetQueryableAsync();
        var nominations = await AsyncExecuter.ToListAsync(
            nomQ.Where(x => x.CasualCourseId == casualCourseId));

        if (!nominations.Any())
            throw new BusinessException("Training:CasualCourse:NoNominations");

    }

    public async Task ValidateForTDApprovalAsync(Guid casualCourseId)
    {
        var cc = await casualCourseRepo.GetAsync(casualCourseId);

        if (cc.Status != CasualCourseStatus.StaffReviewed)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        if (!cc.EstimatedTotalCost.HasValue || cc.EstimatedTotalCost.Value <= 0)
            throw new BusinessException("Training:CasualCourse:CostRequired");

        if (!cc.FundingScenario.HasValue)
            throw new BusinessException("Training:CasualCourse:ScenarioRequired");

        if (cc.IsReturned)
            throw new BusinessException("Training:CasualCourse:UnresolvedReturn");

        var nomQ = await nominationRepo.GetQueryableAsync();
        var anyReturnedNom = await AsyncExecuter.AnyAsync(
            nomQ.Where(x => x.CasualCourseId == casualCourseId && x.IsReturned));
        if (anyReturnedNom)
            throw new BusinessException("Training:CasualCourse:UnresolvedReturnedNominations");
    }

    public async Task ValidateForHeadApprovalAsync(Guid casualCourseId)
    {
        var cc = await casualCourseRepo.GetAsync(casualCourseId);

        if (cc.Status != CasualCourseStatus.TDApproved)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        if (!cc.EstimatedTotalCost.HasValue || cc.EstimatedTotalCost.Value <= 0)
            throw new BusinessException("Training:CasualCourse:CostRequired");
    }
}
