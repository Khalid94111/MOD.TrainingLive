using System.Collections.Generic;
using System.Threading.Tasks;

namespace Travel.TravelRequests;

public class NullPayrollElementLookup : IPayrollElementLookup
{
    public Task<List<PayrollElementLookupDto>> GetPayrollElementsAsync()
    {
        return Task.FromResult(new List<PayrollElementLookupDto>());
    }
}
