using System.Collections.Generic;
using System.Threading.Tasks;

namespace Travel.Tenants;

public class NullTenantLookup : ITenantLookup
{
    public Task<List<TenantLookupDto>> GetTenantsAsync()
    {
        return Task.FromResult(new List<TenantLookupDto>());
    }
}
