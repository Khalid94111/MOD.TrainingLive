using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Oranges;

public partial interface IOrangeRepository : IRepository<Orange, Guid>
{
    Task DeleteAllAsync(string? filterText = null, string? arabicName = null, CancellationToken cancellationToken = default);
    Task<List<Orange>> GetListAsync(string? filterText = null, string? arabicName = null, string? sorting = null, int maxResultCount = int.MaxValue, int skipCount = 0, CancellationToken cancellationToken = default);
    Task<long> GetCountAsync(string? filterText = null, string? arabicName = null, CancellationToken cancellationToken = default);
}