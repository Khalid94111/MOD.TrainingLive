using System;
using Volo.Abp.EventBus;

namespace Travel.TravelRequests;

[EventName("Travel.TrainingRequest.Completed")]
public class TrainingTravelCompletedEto
{
    public Guid? TenantId { get; set; }
    public TrainingTravelResultDto Result { get; set; } = new();
}
