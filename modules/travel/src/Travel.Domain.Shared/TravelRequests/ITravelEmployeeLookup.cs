using System.Collections.Generic;
using System.Threading.Tasks;

namespace Travel.TravelRequests;

public interface ITravelEmployeeLookup
{
    Task<List<TravelEmployeeLookupResult>> FindByEmployeeNumbersAsync(IEnumerable<string> employeeNumbers);
}
