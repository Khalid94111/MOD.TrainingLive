using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Finance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace MOD.Training.Training.Managers;

/// <summary>
/// Mirrors PlanItemRankBreakdownManager for casual-course financials.
/// Creates / refreshes / updates rank sub-rows under CasualCourseFinancial and
/// keeps the parent EstimatedAmountOMR and course EstimatedTotalCost in sync.
/// </summary>
public class CasualCourseRankBreakdownManager(
    IRepository<CasualCourseFinancial, Guid> financialRepo,
    IRepository<CasualCourseFinancialItemRank, Guid> rankRepo,
    IRepository<CasualCourse, Guid> casualCourseRepo,
    IRepository<CasualCourseNomination, Guid> nominationRepo,
    IRepository<FinancialItem, Guid> financialItemRepo,
    EmployeeResolver employeeResolver,
    FinancialItemDefaultResolver defaultResolver)
    : DomainService
{
    /// <summary>
    /// Creates rank rows for a newly-inserted CasualCourseFinancial parent.
    /// Flat items get one synthetic row (RankId = Guid.Empty, NomineeCount = 1).
    /// Per-nominee items get one row per rank present in the nominee list.
    /// </summary>
    public async Task InitializeAsync(Guid financialId)
    {
        var parent = await financialRepo.GetAsync(financialId);
        var fi = await financialItemRepo.GetAsync(parent.FinancialItemId);
        var course = await casualCourseRepo.GetAsync(parent.CasualCourseId);

        if (!fi.IsPerNominee)
        {
            var (rate, source) = await defaultResolver.ResolveRateWithSourceAsync(fi.Id, rankId: null);
            var subtotal = defaultResolver.ComputeSubtotal(
                rate, fi.IsPerDay, fi.IsPerNominee,
                course.DurationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, nomineeCount: 1);

            await rankRepo.InsertAsync(
                new CasualCourseFinancialItemRank(
                    GuidGenerator.Create(), parent.Id, Guid.Empty, 1, rate, subtotal, source),
                autoSave: true);
        }
        else
        {
            var rankCounts = await GetRankCountsAsync(parent.CasualCourseId);
            foreach (var (rankId, count) in rankCounts)
            {
                var (rate, source) = await defaultResolver.ResolveRateWithSourceAsync(fi.Id, rankId);
                var subtotal = defaultResolver.ComputeSubtotal(
                    rate, fi.IsPerDay, fi.IsPerNominee,
                    course.DurationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, count);

                await rankRepo.InsertAsync(
                    new CasualCourseFinancialItemRank(
                        GuidGenerator.Create(), parent.Id, rankId, count, rate, subtotal, source),
                    autoSave: true);
            }
        }

        await RefreshParentTotalAsync(parent);
    }

    /// <summary>
    /// Inline edit of one rank row's rate. Recomputes its subtotal and the parent total.
    /// </summary>
    public async Task UpdateRateAsync(Guid rankRowId, decimal newRate)
    {
        var row = await rankRepo.GetAsync(rankRowId);
        var parent = await financialRepo.GetAsync(row.CasualCourseFinancialId);
        var fi = await financialItemRepo.GetAsync(parent.FinancialItemId);
        var course = await casualCourseRepo.GetAsync(parent.CasualCourseId);

        row.RatePerUnitOMR = newRate;
        row.SubtotalOMR = defaultResolver.ComputeSubtotal(
            newRate, fi.IsPerDay, fi.IsPerNominee,
            course.DurationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, row.NomineeCount);
        await rankRepo.UpdateAsync(row, autoSave: true);

        await RefreshParentTotalAsync(parent);
    }

    /// <summary>
    /// Re-derives rank row structure (counts and add/remove) from the current nominee list.
    /// Rates on existing rows are preserved; new rows default via the resolver.
    /// </summary>
    public async Task RefreshForNomineeChangeAsync(Guid casualCourseId)
    {
        var course = await casualCourseRepo.GetAsync(casualCourseId);
        var rankCounts = await GetRankCountsAsync(casualCourseId);

        var parentQ = await financialRepo.GetQueryableAsync();
        var parents = await AsyncExecuter.ToListAsync(parentQ.Where(x => x.CasualCourseId == casualCourseId));

        foreach (var parent in parents)
        {
            var fi = await financialItemRepo.GetAsync(parent.FinancialItemId);
            if (!fi.IsPerNominee) continue;

            var rankQ = await rankRepo.GetQueryableAsync();
            var existing = (await AsyncExecuter.ToListAsync(
                rankQ.Where(x => x.CasualCourseFinancialId == parent.Id)))
                .ToDictionary(x => x.RankId);

            foreach (var (rankId, count) in rankCounts)
            {
                if (existing.TryGetValue(rankId, out var row))
                {
                    row.NomineeCount = count;
                    row.SubtotalOMR = defaultResolver.ComputeSubtotal(
                        row.RatePerUnitOMR, fi.IsPerDay, fi.IsPerNominee,
                        course.DurationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, count);
                    await rankRepo.UpdateAsync(row);
                }
                else
                {
                    var (rate, source) = await defaultResolver.ResolveRateWithSourceAsync(fi.Id, rankId);
                    var subtotal = defaultResolver.ComputeSubtotal(
                        rate, fi.IsPerDay, fi.IsPerNominee,
                        course.DurationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, count);
                    await rankRepo.InsertAsync(
                        new CasualCourseFinancialItemRank(
                            GuidGenerator.Create(), parent.Id, rankId, count, rate, subtotal, source));
                }
            }

            foreach (var orphan in existing.Where(e => !rankCounts.ContainsKey(e.Key)).Select(e => e.Value))
                await rankRepo.DeleteAsync(orphan);

            await RefreshParentTotalAsync(parent);
        }
    }

    /// <summary>
    /// Re-reads rank rows for a parent and writes the sum into parent.EstimatedAmountOMR,
    /// then refreshes the course's EstimatedTotalCost.
    /// </summary>
    public async Task RefreshParentTotalAsync(CasualCourseFinancial parent)
    {
        var rankQ = await rankRepo.GetQueryableAsync();
        var rows = await AsyncExecuter.ToListAsync(
            rankQ.Where(x => x.CasualCourseFinancialId == parent.Id));
        parent.EstimatedAmountOMR = rows.Sum(x => x.SubtotalOMR);
        await financialRepo.UpdateAsync(parent, autoSave: true);

        await RefreshCourseTotalAsync(parent.CasualCourseId);
    }

    public async Task RefreshCourseTotalAsync(Guid casualCourseId)
    {
        var parentQ = await financialRepo.GetQueryableAsync();
        var parents = await AsyncExecuter.ToListAsync(parentQ.Where(x => x.CasualCourseId == casualCourseId));
        var sum = parents.Sum(x => x.EstimatedAmountOMR);

        var course = await casualCourseRepo.GetAsync(casualCourseId);
        course.EstimatedTotalCost = sum;
        await casualCourseRepo.UpdateAsync(course, autoSave: true);
    }

    private async Task<Dictionary<Guid, int>> GetRankCountsAsync(Guid casualCourseId)
    {
        var nomQ = await nominationRepo.GetQueryableAsync();
        var employeeIds = await AsyncExecuter.ToListAsync(
            nomQ.Where(x => x.CasualCourseId == casualCourseId).Select(x => x.EmployeeId));

        if (employeeIds.Count == 0) return new Dictionary<Guid, int>();

        var employees = await employeeResolver.GetEmployeesWithRanksAsync(employeeIds);
        return employees
            .GroupBy(e => e.RankId)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
