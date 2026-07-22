using System.Collections.Generic;
using System.Threading.Tasks;

namespace Travel.TravelRequests;

public class NullCountryLookup : ICountryLookup
{
    public Task<List<CountryLookupDto>> GetCountriesAsync()
    {
        return Task.FromResult(new List<CountryLookupDto>());
    }
}
