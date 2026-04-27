using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.CasualCourses.Dtos;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.CasualCourses;

[Authorize(TrainingPermissions.CasualCourses.Review)]
public class CasualCourseFinancialItemRankAppService(
    IRepository<CasualCourseFinancialItemRank, Guid> rankRepo,
    IRepository<CasualCourseFinancialItem, Guid> financialRepo,
    IRepository<CasualCourse, Guid> casualCourseRepo,
    CasualCourseRankBreakdownManager rankManager,
    ICasualCourseFinancialItemAppService financialAppService)
    : ApplicationService, ICasualCourseFinancialItemRankAppService
{
    public async Task<CasualCourseFinancialItemDto> UpdateRateAsync(Guid id, UpdateRankRateDto input)
    {
        var row = await rankRepo.GetAsync(id);
        var parent = await financialRepo.GetAsync(row.CasualCourseFinancialItemId);
        var cc = await casualCourseRepo.GetAsync(parent.CasualCourseId);

        if (cc.Status != CasualCourseStatus.UnderReview)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        await rankManager.UpdateRateAsync(id, input.RatePerUnitOMR);

        var all = await financialAppService.GetListByCasualCourseAsync(parent.CasualCourseId);
        return all.Single(x => x.Id == parent.Id);
    }
}
