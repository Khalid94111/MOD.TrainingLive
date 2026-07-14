using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.CasualCourses.Dtos;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Hr;
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

[Authorize(TrainingPermissions.CasualCourses.Default)]
public class CasualCourseFinancialItemAppService(
    IRepository<CasualCourseFinancialItem, Guid> repository,
    IRepository<CasualCourseFinancialItemRank, Guid> rankRepo,
    IRepository<CasualCourse, Guid> casualCourseRepo,
    IRepository<FinancialItem, Guid> financialItemRepo,
    IRepository<Rank, Guid> rankRefRepo,
    FinancialItemDefaultResolver rateResolver,
    FundingScenarioSourceResolver scenarioSourceResolver,
    CasualCourseRankBreakdownManager rankManager,
    CasualCourseFinancialItemToDtoMapper toDtoMapper,
    CasualCourseFinancialItemRankToDtoMapper rankToDtoMapper)
    : ApplicationService, ICasualCourseFinancialItemAppService
{
    public async Task<List<CasualCourseFinancialItemDto>> GetListByCasualCourseAsync(Guid casualCourseId)
    {
        var q = await repository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(q.Where(x => x.CasualCourseId == casualCourseId));
        if (items.Count == 0) return new List<CasualCourseFinancialItemDto>();

        return await HydrateAsync(items, casualCourseId);
    }

    [Authorize(TrainingPermissions.CasualCourses.Review)]
    public async Task<CasualCourseFinancialItemDto> AddItemAsync(
        Guid casualCourseId, CreateCasualCourseFinancialItemDto input)
    {
        var cc = await casualCourseRepo.GetAsync(casualCourseId);
        if (cc.Status != CasualCourseStatus.UnderReview)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        FinancialAmountSource src;
        if (cc.FundingScenario.HasValue)
        {
            var fi = await financialItemRepo.GetAsync(input.FinancialItemId);
            src = scenarioSourceResolver.Resolve(cc.FundingScenario.Value, fi);
        }
        else
        {
            src = FinancialAmountSource.FinancialItem;
        }

        var parent = await repository.InsertAsync(
            new CasualCourseFinancialItem(
                GuidGenerator.Create(),
                casualCourseId,
                input.FinancialItemId,
                estimatedAmount: 0m,
                src)
            {
                Notes = input.Notes,
            },
            autoSave: true);

        await rankManager.InitializeAsync(parent.Id);

        var list = await HydrateAsync(new List<CasualCourseFinancialItem> { parent }, casualCourseId);
        return list.Single();
    }

    [Authorize(TrainingPermissions.CasualCourses.Review)]
    public async Task DeleteItemAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        var cc = await casualCourseRepo.GetAsync(entity.CasualCourseId);
        if (cc.Status != CasualCourseStatus.UnderReview)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        var parentId = entity.CasualCourseId;
        await repository.DeleteAsync(entity, autoSave: true);  // cascade deletes rank rows
        await rankManager.RefreshCourseTotalAsync(parentId);
    }

    private async Task<List<CasualCourseFinancialItemDto>> HydrateAsync(
        List<CasualCourseFinancialItem> parents, Guid casualCourseId)
    {
        var fiIds = parents.Select(x => x.FinancialItemId).Distinct().ToList();
        var fiQ = await financialItemRepo.GetQueryableAsync();
        var fis = (await AsyncExecuter.ToListAsync(fiQ.Where(x => fiIds.Contains(x.Id))))
            .ToDictionary(x => x.Id);

        var parentIds = parents.Select(x => x.Id).ToList();
        var rankQ = await rankRepo.GetQueryableAsync();
        var rankRows = await AsyncExecuter.ToListAsync(
            rankQ.Where(x => parentIds.Contains(x.CasualCourseFinancialItemId)));
        var ranksByParent = rankRows.GroupBy(r => r.CasualCourseFinancialItemId).ToDictionary(g => g.Key, g => g.ToList());

        var rankIds = rankRows.Select(r => r.RankId).Where(id => id != Guid.Empty).Distinct().ToList();
        var rankLookup = new Dictionary<Guid, string>();
        if (rankIds.Count > 0)
        {
            var rQ = await rankRefRepo.GetQueryableAsync();
            var ranks = await AsyncExecuter.ToListAsync(rQ.Where(x => rankIds.Contains(x.Id)));
            rankLookup = ranks.ToDictionary(x => x.Id, x => x.NameAr);
        }

        var cc = await casualCourseRepo.GetAsync(casualCourseId);
        var duration = cc.DurationDays;

        return parents
            .OrderBy(p => fis.TryGetValue(p.FinancialItemId, out var fi) ? fi.NameAr : "")
            .Select(e =>
            {
                var dto = toDtoMapper.Map(e);
                fis.TryGetValue(e.FinancialItemId, out var fi);
                if (fi != null)
                {
                    dto.FinancialItemName = fi.NameAr;
                    dto.IsPerDay = fi.IsPerDay;
                    dto.IsPerNominee = fi.IsPerNominee;
                    dto.ExtraDaysBefore = fi.ExtraDaysBefore;
                    dto.ExtraDaysAfter = fi.ExtraDaysAfter;
                    dto.EffectiveDays = fi.IsPerDay
                        ? duration + fi.ExtraDaysBefore + fi.ExtraDaysAfter
                        : 1;
                }
                if (ranksByParent.TryGetValue(e.Id, out var rows))
                {
                    dto.Ranks = rows
                        .OrderBy(r => r.RankId == Guid.Empty ? 0 : 1)
                        .ThenBy(r => rankLookup.GetValueOrDefault(r.RankId) ?? "")
                        .Select(r =>
                        {
                            var rdto = rankToDtoMapper.Map(r);
                            rdto.RankNameAr = r.RankId == Guid.Empty
                                ? "ثابت"
                                : rankLookup.GetValueOrDefault(r.RankId) ?? "";
                            return rdto;
                        })
                        .ToList();
                }
                return dto;
            })
            .ToList();
    }
}
