using MOD.Training.Training.Finance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace MOD.Training.Training.Managers;

public class FinancialItemDefaultResolver(
    IRepository<FinancialItemRankAmount, Guid> rankAmountRepo,
    IRepository<FinancialItem, Guid> financialItemRepo)
    : DomainService
{
    public const string RateSourceRankOverride = "RankOverride";
    public const string RateSourceDefaultAmount = "DefaultAmount";

    public async Task<decimal> ResolveRateAsync(Guid financialItemId, Guid rankId)
    {
        var q = await rankAmountRepo.GetQueryableAsync();
        var rankAmount = await AsyncExecuter.FirstOrDefaultAsync(
            q.Where(x => x.FinancialItemId == financialItemId && x.RankId == rankId));
        if (rankAmount != null) return rankAmount.AmountOMR;

        var fi = await financialItemRepo.GetAsync(financialItemId);
        return fi.DefaultAmountOMR;
    }

    /// <summary>
    /// Resolves the rate for a financial item, optionally considering a rank override.
    /// Returns the rate and its source ("RankOverride" | "DefaultAmount").
    /// Passing <paramref name="rankId"/> as null skips the rank override lookup entirely
    /// (used for flat/non-per-nominee items).
    /// </summary>
    public async Task<(decimal Rate, string Source)> ResolveRateWithSourceAsync(
        Guid financialItemId, Guid? rankId)
    {
        if (rankId.HasValue)
        {
            var q = await rankAmountRepo.GetQueryableAsync();
            var rankAmount = await AsyncExecuter.FirstOrDefaultAsync(
                q.Where(x => x.FinancialItemId == financialItemId && x.RankId == rankId.Value));
            if (rankAmount != null)
                return (rankAmount.AmountOMR, RateSourceRankOverride);
        }

        var fi = await financialItemRepo.GetAsync(financialItemId);
        return (fi.DefaultAmountOMR, RateSourceDefaultAmount);
    }

    public async Task<Dictionary<Guid, decimal>> ResolveRatesAsync(
        Guid financialItemId, IEnumerable<Guid> rankIds)
    {
        var rankIdList = rankIds.Distinct().ToList();
        var q = await rankAmountRepo.GetQueryableAsync();
        var ranks = await AsyncExecuter.ToListAsync(
            q.Where(x => x.FinancialItemId == financialItemId && rankIdList.Contains(x.RankId)));

        var result = ranks.ToDictionary(x => x.RankId, x => x.AmountOMR);

        var fi = await financialItemRepo.GetAsync(financialItemId);
        foreach (var rid in rankIdList.Where(r => !result.ContainsKey(r)))
            result[rid] = fi.DefaultAmountOMR;

        return result;
    }

    public decimal ComputeSubtotal(
        decimal rate, bool isPerDay, bool isPerNominee,
        int courseDays, int extraBefore, int extraAfter, int nomineeCount)
    {
        var effectiveDays = isPerDay ? (courseDays + extraBefore + extraAfter) : 1;
        var effectiveCount = isPerNominee ? nomineeCount : 1;
        return rate * effectiveDays * effectiveCount;
    }
}
