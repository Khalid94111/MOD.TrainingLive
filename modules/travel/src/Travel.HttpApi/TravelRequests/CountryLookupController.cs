using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travel.Permissions;
using Volo.Abp;

namespace Travel.TravelRequests;

[Area(TravelRemoteServiceConsts.ModuleName)]
[RemoteService(Name = TravelRemoteServiceConsts.RemoteServiceName)]
[Route("api/travel/countries")]
[Authorize(TravelManagementPermissions.TravelRequests.Default)]
public class CountryLookupController : TravelController
{
    private readonly ICountryLookup _countryLookup;

    public CountryLookupController(ICountryLookup countryLookup)
    {
        _countryLookup = countryLookup;
    }

    [HttpGet]
    public Task<List<CountryLookupDto>> GetCountriesAsync()
    {
        return _countryLookup.GetCountriesAsync();
    }
}
