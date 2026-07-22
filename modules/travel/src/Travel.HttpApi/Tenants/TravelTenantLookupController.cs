using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Travel.Tenants;

[Area(TravelRemoteServiceConsts.ModuleName)]
[RemoteService(Name = TravelRemoteServiceConsts.RemoteServiceName)]
[Route("api/travel/tenants")]
public class TravelTenantLookupController : TravelController
{
    private readonly ITenantLookup _tenantLookup;

    public TravelTenantLookupController(ITenantLookup tenantLookup)
    {
        _tenantLookup = tenantLookup;
    }

    [HttpGet]
    public virtual Task<List<TenantLookupDto>> GetTenantsAsync()
    {
        return _tenantLookup.GetTenantsAsync();
    }
}
