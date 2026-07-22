using System;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace MOD.Training.Training.Travel.Integration;

/// <summary>
/// Safe fallback while the Travel module is being installed. The real Travel adapter replaces
/// this service; returning an explicit availability flag keeps session reads functional.
/// </summary>
[Dependency(TryRegister = true)]
public class UnavailableTrainingTravelGateway : ITrainingTravelGateway, ITransientDependency
{
    public Task<TrainingTravelGatewayResult> GetByTrainingCourseAsync(Guid trainingCourseId)
        => Task.FromResult(new TrainingTravelGatewayResult
        {
            IsAvailable = false,
            IsFound = false,
            StatusCode = "NotConfigured"
        });

    public Task<TrainingTravelGatewayResult> CreateFromTrainingAsync(TrainingTravelGatewayRequest input)
        => Task.FromResult(new TrainingTravelGatewayResult
        {
            IsAvailable = false,
            IsFound = false,
            StatusCode = "NotConfigured"
        });
}
