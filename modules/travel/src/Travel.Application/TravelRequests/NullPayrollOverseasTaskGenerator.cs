using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Travel.TravelRequests;

public class NullPayrollOverseasTaskGenerator : IPayrollOverseasTaskGenerator
{
    private readonly ILogger<NullPayrollOverseasTaskGenerator> _logger;

    public NullPayrollOverseasTaskGenerator(ILogger<NullPayrollOverseasTaskGenerator> logger)
    {
        _logger = logger;
    }

    public Task GenerateAsync(TravelRequest request, Guid payrollElementId)
    {
        _logger.LogWarning(
            "No IPayrollOverseasTaskGenerator implementation is configured; skipping overseas payroll task generation for travel request {RequestId}.",
            request.Id);

        return Task.CompletedTask;
    }
}
