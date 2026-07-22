using System.Collections.Generic;
using System.Threading.Tasks;

namespace Travel.Tenants;

public interface ITenantLookup
{
    Task<List<TenantLookupDto>> GetTenantsAsync();
}
