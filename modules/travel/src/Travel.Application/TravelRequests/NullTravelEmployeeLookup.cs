using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Travel.TravelRequests;

public class NullTravelEmployeeLookup : ITravelEmployeeLookup
{
    public Task<List<TravelEmployeeLookupResult>> FindByEmployeeNumbersAsync(IEnumerable<string> employeeNumbers)
    {
        return Task.FromResult(new List<TravelEmployeeLookupResult>());
    }
}
