using System.Collections.Generic;
using System.Threading.Tasks;

namespace Travel.TravelRequests;

public interface ICountryLookup
{
    Task<List<CountryLookupDto>> GetCountriesAsync();
}
