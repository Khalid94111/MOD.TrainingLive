using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travel.Permissions;
using Volo.Abp;

namespace Travel.TravelRequests;

[Area(TravelRemoteServiceConsts.ModuleName)]
[RemoteService(Name = TravelRemoteServiceConsts.RemoteServiceName)]
[Route("api/travel/employees")]
[Authorize(TravelManagementPermissions.TravelRequests.Default)]
public class EmployeeLookupController : TravelController
{
    private readonly ITravelEmployeeLookup _employeeLookup;

    public EmployeeLookupController(ITravelEmployeeLookup employeeLookup)
    {
        _employeeLookup = employeeLookup;
    }

    [HttpPost("find")]
    public Task<List<TravelEmployeeLookupResult>> FindByEmployeeNumbersAsync(
        [FromBody] List<string> employeeNumbers)
    {
        return _employeeLookup.FindByEmployeeNumbersAsync(employeeNumbers);
    }
}
