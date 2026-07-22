using System.Collections.Generic;
using System.Threading.Tasks;

namespace Travel.TravelRequests;

/// <summary>
/// Provides the payroll elements that can be selected when a long travel request
/// is transferred to the payroll system. Implemented by the host application.
/// </summary>
public interface IPayrollElementLookup
{
    Task<List<PayrollElementLookupDto>> GetPayrollElementsAsync();
}
