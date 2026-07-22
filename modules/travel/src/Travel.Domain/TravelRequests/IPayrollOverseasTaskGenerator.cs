using System;
using System.Threading.Tasks;

namespace Travel.TravelRequests;

/// <summary>
/// Creates overseas payroll tasks for the travel days that exceed the allowance
/// period covered by the travel module. Implemented by the host application.
/// </summary>
public interface IPayrollOverseasTaskGenerator
{
    Task GenerateAsync(TravelRequest request, Guid payrollElementId);
}
