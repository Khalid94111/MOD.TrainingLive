using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using MOD.Training.EntityFrameworkCore;

namespace MOD.Training.Oranges;

public abstract class EfCoreOrangeRepositoryBase : EfCoreRepository<TrainingDbContext, Orange, Guid>
{
    public EfCoreOrangeRepositoryBase(IDbContextProvider<TrainingDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }

    public virtual async Task DeleteAllAsync(string? filterText = null, string? arabicName = null, CancellationToken cancellationToken = default)
    {
        var query = await GetQueryableAsync();
        query = ApplyFilter(query, filterText, arabicName);
        var ids = query.Select(x => x.Id);
        await DeleteManyAsync(ids, cancellationToken: GetCancellationToken(cancellationToken));
    }

    public virtual async Task<List<Orange>> GetListAsync(string? filterText = null, string? arabicName = null, string? sorting = null, int maxResultCount = int.MaxValue, int skipCount = 0, CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter((await GetQueryableAsync()), filterText, arabicName);
        query = query.OrderBy(string.IsNullOrWhiteSpace(sorting) ? OrangeConsts.GetDefaultSorting(false) : sorting);
        return await query.PageBy(skipCount, maxResultCount).ToListAsync(cancellationToken);
    }

    public virtual async Task<long> GetCountAsync(string? filterText = null, string? arabicName = null, CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter((await GetDbSetAsync()), filterText, arabicName);
        return await query.LongCountAsync(GetCancellationToken(cancellationToken));
    }

    protected virtual IQueryable<Orange> ApplyFilter(IQueryable<Orange> query, string? filterText = null, string? arabicName = null)
    {
        return query.WhereIf(!string.IsNullOrWhiteSpace(filterText), e => e.ArabicName!.Contains(filterText!)).WhereIf(!string.IsNullOrWhiteSpace(arabicName), e => e.ArabicName.Contains(arabicName));
    }
}