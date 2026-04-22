using MOD.Training.Training.Finance;
using MOD.Training.Training.Nominations;
using MOD.Training.Training.Plans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace MOD.Training.Training.Managers;

public class PlanItemRankBreakdownManager(
    IRepository<PlanItemFinancialItem, Guid> pifiRepo,
    IRepository<PlanItemFinancialItemRank, Guid> rankRepo,
    IRepository<FinancialItem, Guid> fiRepo,
    IRepository<TrainingPlanItem, Guid> planItemRepo,
    IRepository<Nomination, Guid> nominationRepo,
    EmployeeResolver employeeResolver,
    FinancialItemDefaultResolver defaultResolver)
    : DomainService
{
    public async Task InitializeAsync(Guid planItemFinancialItemId)
    {
        var pifi = await pifiRepo.GetAsync(planItemFinancialItemId);
        var fi = await fiRepo.GetAsync(pifi.FinancialItemId);
        if (!fi.IsPerNominee) return;

        var planItem = await planItemRepo.GetAsync(pifi.PlanItemId);
        var rankCounts = await GetRankCountsAsync(pifi.PlanItemId);

        foreach (var (rankId, count) in rankCounts)
        {
            var rate = await defaultResolver.ResolveRateAsync(fi.Id, rankId);
            var subtotal = defaultResolver.ComputeSubtotal(
                rate, fi.IsPerDay, fi.IsPerNominee,
                planItem.DurationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, count);

            await rankRepo.InsertAsync(
                new PlanItemFinancialItemRank(
                    GuidGenerator.Create(), pifi.Id, rankId, count, rate, subtotal),
                autoSave: true);
        }

        await RefreshPifiTotalAsync(pifi);
    }

    public async Task RefreshForDaysChangeAsync(Guid planItemId)
    {
        var planItem = await planItemRepo.GetAsync(planItemId);
        var pifiQ = await pifiRepo.GetQueryableAsync();
        var pifis = await AsyncExecuter.ToListAsync(
            pifiQ.Where(x => x.PlanItemId == planItemId));

        foreach (var pifi in pifis)
        {
            var fi = await fiRepo.GetAsync(pifi.FinancialItemId);
            if (!fi.IsPerDay) continue;

            var rankQ = await rankRepo.GetQueryableAsync();
            var ranks = await AsyncExecuter.ToListAsync(
                rankQ.Where(x => x.PlanItemFinancialItemId == pifi.Id));

            foreach (var r in ranks)
            {
                r.SubtotalOMR = defaultResolver.ComputeSubtotal(
                    r.RatePerUnitOMR, fi.IsPerDay, fi.IsPerNominee,
                    planItem.DurationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, r.NomineeCount);
                await rankRepo.UpdateAsync(r);
            }

            await RefreshPifiTotalAsync(pifi);
        }
    }

    public async Task RefreshForNomineeChangeAsync(Guid planItemId)
    {
        var planItem = await planItemRepo.GetAsync(planItemId);
        var rankCounts = await GetRankCountsAsync(planItemId);

        var pifiQ = await pifiRepo.GetQueryableAsync();
        var pifis = await AsyncExecuter.ToListAsync(
            pifiQ.Where(x => x.PlanItemId == planItemId));

        foreach (var pifi in pifis)
        {
            var fi = await fiRepo.GetAsync(pifi.FinancialItemId);
            if (!fi.IsPerNominee) continue;

            var rankQ = await rankRepo.GetQueryableAsync();
            var existing = (await AsyncExecuter.ToListAsync(
                rankQ.Where(x => x.PlanItemFinancialItemId == pifi.Id)))
                .ToDictionary(x => x.RankId);

            foreach (var (rankId, count) in rankCounts)
            {
                if (existing.TryGetValue(rankId, out var row))
                {
                    row.NomineeCount = count;
                    row.SubtotalOMR = defaultResolver.ComputeSubtotal(
                        row.RatePerUnitOMR, fi.IsPerDay, fi.IsPerNominee,
                        planItem.DurationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, count);
                    await rankRepo.UpdateAsync(row);
                }
                else
                {
                    var rate = await defaultResolver.ResolveRateAsync(fi.Id, rankId);
                    var subtotal = defaultResolver.ComputeSubtotal(
                        rate, fi.IsPerDay, fi.IsPerNominee,
                        planItem.DurationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, count);
                    await rankRepo.InsertAsync(
                        new PlanItemFinancialItemRank(
                            GuidGenerator.Create(), pifi.Id, rankId, count, rate, subtotal));
                }
            }

            // Remove rows for ranks no longer present
            foreach (var (rankId, row) in existing.Where(e => !rankCounts.ContainsKey(e.Key)))
                await rankRepo.DeleteAsync(row);

            await RefreshPifiTotalAsync(pifi);
        }
    }

    public async Task UpdateRateAsync(Guid rankRowId, decimal newRate)
    {
        var row = await rankRepo.GetAsync(rankRowId);
        var pifi = await pifiRepo.GetAsync(row.PlanItemFinancialItemId);
        var fi = await fiRepo.GetAsync(pifi.FinancialItemId);
        var planItem = await planItemRepo.GetAsync(pifi.PlanItemId);

        row.RatePerUnitOMR = newRate;
        row.SubtotalOMR = defaultResolver.ComputeSubtotal(
            newRate, fi.IsPerDay, fi.IsPerNominee,
            planItem.DurationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, row.NomineeCount);
        await rankRepo.UpdateAsync(row);

        await RefreshPifiTotalAsync(pifi);
    }

    private async Task<Dictionary<Guid, int>> GetRankCountsAsync(Guid planItemId)
    {
        var nomQ = await nominationRepo.GetQueryableAsync();
        var employeeIds = await AsyncExecuter.ToListAsync(
            nomQ.Where(x => x.PlanItemId == planItemId && !x.IsReturned)
                .Select(x => x.EmployeeId));

        var employees = await employeeResolver.BatchResolveByIdsAsync(employeeIds);
        return employees.Values
            .GroupBy(e => e.RankId)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private async Task RefreshPifiTotalAsync(PlanItemFinancialItem pifi)
    {
        var rankQ = await rankRepo.GetQueryableAsync();
        var rows = await AsyncExecuter.ToListAsync(
            rankQ.Where(x => x.PlanItemFinancialItemId == pifi.Id));
        var sum = rows.Sum(x => x.SubtotalOMR);

        if (sum > 0)
        {
            pifi.EstimatedAmountOMR = sum;
            await pifiRepo.UpdateAsync(pifi);
        }
    }
}
