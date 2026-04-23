using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.CasualCourses.Dtos;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
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
public class CasualCourseFinancialAppService(
    IRepository<CasualCourseFinancial, Guid> repository,
    IRepository<CasualCourse, Guid> casualCourseRepo,
    IRepository<CasualCourseNomination, Guid> nominationRepo,
    IRepository<CourseTypeFinancialItemDefault, Guid> defaultsRepo,
    IRepository<FinancialItem, Guid> financialItemRepo,
    FinancialItemDefaultResolver defaultResolver,
    CasualCourseFinancialToDtoMapper toDtoMapper)
    : ApplicationService, ICasualCourseFinancialAppService
{
    public async Task<List<CasualCourseFinancialDto>> GetListByCasualCourseAsync(Guid casualCourseId)
    {
        var q = await repository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(q.Where(x => x.CasualCourseId == casualCourseId));

        var fiIds = items.Select(x => x.FinancialItemId).Distinct().ToList();
        var fiQ = await financialItemRepo.GetQueryableAsync();
        var fis = await AsyncExecuter.ToListAsync(fiQ.Where(x => fiIds.Contains(x.Id)));
        var fiMap = fis.ToDictionary(x => x.Id);

        return items
            .OrderBy(x => fiMap.TryGetValue(x.FinancialItemId, out var fi) ? fi.NameAr : "")
            .Select(e =>
            {
                var dto = toDtoMapper.Map(e);
                if (fiMap.TryGetValue(e.FinancialItemId, out var fi))
                {
                    dto.FinancialItemName = fi.NameAr;
                    dto.IsPerDay = fi.IsPerDay;
                    dto.IsPerNominee = fi.IsPerNominee;
                }
                return dto;
            })
            .ToList();
    }

    [Authorize(TrainingPermissions.CasualCourses.Review)]
    public async Task<List<CasualCourseFinancialDto>> AutoFillFromDefaultsAsync(Guid casualCourseId)
    {
        var cc = await casualCourseRepo.GetAsync(casualCourseId);
        if (cc.Status != CasualCourseStatus.UnderReview)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        var nomQ = await nominationRepo.GetQueryableAsync();
        var nomineeCount = await AsyncExecuter.CountAsync(nomQ.Where(x => x.CasualCourseId == casualCourseId));

        var defQ = await defaultsRepo.WithDetailsAsync(x => x.FinancialItem);
        var defaults = await AsyncExecuter.ToListAsync(
            defQ.Where(x => x.CourseType == cc.CourseType).OrderBy(x => x.SortOrder));

        var existingQ = await repository.GetQueryableAsync();
        var existingFiIds = (await AsyncExecuter.ToListAsync(
            existingQ.Where(x => x.CasualCourseId == casualCourseId)))
            .Select(x => x.FinancialItemId)
            .ToHashSet();

        foreach (var def in defaults.Where(d => !existingFiIds.Contains(d.FinancialItemId)))
        {
            var fi = def.FinancialItem;
            var amount = defaultResolver.ComputeSubtotal(
                fi.DefaultAmountOMR, fi.IsPerDay, fi.IsPerNominee,
                cc.DurationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, nomineeCount);

            await repository.InsertAsync(
                new CasualCourseFinancial(
                    GuidGenerator.Create(),
                    casualCourseId,
                    fi.Id,
                    amount,
                    FinancialAmountSource.FundingSource),
                autoSave: true);
        }

        await RecomputeTotalAsync(casualCourseId);
        return await GetListByCasualCourseAsync(casualCourseId);
    }

    [Authorize(TrainingPermissions.CasualCourses.Review)]
    public async Task<CasualCourseFinancialDto> AddItemAsync(
        Guid casualCourseId, CreateCasualCourseFinancialDto input)
    {
        var cc = await casualCourseRepo.GetAsync(casualCourseId);
        if (cc.Status != CasualCourseStatus.UnderReview)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        var entity = new CasualCourseFinancial(
            GuidGenerator.Create(),
            casualCourseId,
            input.FinancialItemId,
            input.EstimatedAmountOMR,
            FinancialAmountSource.FundingSource)
        {
            Notes = input.Notes,
        };
        await repository.InsertAsync(entity, autoSave: true);

        await RecomputeTotalAsync(casualCourseId);
        return toDtoMapper.Map(entity);
    }

    [Authorize(TrainingPermissions.CasualCourses.Review)]
    public async Task<CasualCourseFinancialDto> UpdateAmountAsync(Guid id, UpdateAmountDto input)
    {
        var entity = await repository.GetAsync(id);
        var cc = await casualCourseRepo.GetAsync(entity.CasualCourseId);
        if (cc.Status != CasualCourseStatus.UnderReview)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        entity.EstimatedAmountOMR = input.Amount;
        entity.Notes = input.Notes;
        await repository.UpdateAsync(entity, autoSave: true);

        await RecomputeTotalAsync(entity.CasualCourseId);
        return toDtoMapper.Map(entity);
    }

    [Authorize(TrainingPermissions.CasualCourses.Review)]
    public async Task DeleteItemAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        var cc = await casualCourseRepo.GetAsync(entity.CasualCourseId);
        if (cc.Status != CasualCourseStatus.UnderReview)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        var parentId = entity.CasualCourseId;
        await repository.DeleteAsync(entity, autoSave: true);
        await RecomputeTotalAsync(parentId);
    }

    private async Task RecomputeTotalAsync(Guid casualCourseId)
    {
        var q = await repository.GetQueryableAsync();
        var total = await AsyncExecuter.ToListAsync(q.Where(x => x.CasualCourseId == casualCourseId));
        var sum = total.Sum(x => x.EstimatedAmountOMR);

        var cc = await casualCourseRepo.GetAsync(casualCourseId);
        cc.EstimatedTotalCost = sum;
        await casualCourseRepo.UpdateAsync(cc, autoSave: true);
    }
}
