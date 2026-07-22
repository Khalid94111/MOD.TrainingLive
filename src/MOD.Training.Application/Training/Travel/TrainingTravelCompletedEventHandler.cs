using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Plans;
using MOD.Training.Training.Travel.Integration;
using Travel.TravelRequests;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Travel;

/// <summary>
/// Imports Travel's final calculation as soon as a training request is completed.
/// The explicit refresh endpoints remain available as a recovery path if the
/// automatic import cannot find the originating training record.
/// </summary>
public class TrainingTravelCompletedEventHandler(
    IRepository<CourseSession, Guid> sessionRepo,
    IRepository<CasualCourse, Guid> casualCourseRepo,
    SessionTravelPaymentSynchronizer paymentSynchronizer,
    ICurrentTenant currentTenant,
    ILogger<TrainingTravelCompletedEventHandler> logger)
    : IDistributedEventHandler<TrainingTravelCompletedEto>, ITransientDependency
{
    public async Task HandleEventAsync(TrainingTravelCompletedEto eventData)
    {
        try
        {
            using (currentTenant.Change(eventData.TenantId))
            {
                var trainingCourseId = eventData.Result.TrainingCourseId;
                var result = TravelTrainingGateway.MapResult(eventData.Result);

                if (!result.IsCompleted)
                {
                    return;
                }

                if (await sessionRepo.FindAsync(trainingCourseId) != null)
                {
                    await paymentSynchronizer.SyncAsync(trainingCourseId, result);
                    return;
                }

                if (await casualCourseRepo.FindAsync(trainingCourseId) != null)
                {
                    await paymentSynchronizer.SyncCasualCourseAsync(trainingCourseId, result);
                    return;
                }

                logger.LogWarning(
                    "Completed Travel request {TravelRequestId} references unknown training course {TrainingCourseId}",
                    result.TravelRequestId,
                    trainingCourseId);
            }
        }
        catch (Exception exception)
        {
            // Completing the Travel request must not be rolled back because of a
            // Training-side import issue. Users can safely retry through Refresh.
            logger.LogError(
                exception,
                "Could not import completed Travel request {TravelRequestId} for training course {TrainingCourseId}",
                eventData.Result.TravelRequestId,
                eventData.Result.TrainingCourseId);
        }
    }
}
