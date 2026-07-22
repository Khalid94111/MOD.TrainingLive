using System;
using Travel.TravelTypes;
using Volo.Abp.EventBus;

namespace Travel.TravelRequests.Events;

[EventName("Travel.TravelRequest.StatusChanged")]
public class TravelRequestStatusChangedEvent
{
    public Guid TravelRequestId { get; }
    public RequestStatus OldStatus { get; }
    public RequestStatus NewStatus { get; }
    public string? Reason { get; }

    public TravelRequestStatusChangedEvent(TravelRequest travelRequest, RequestStatus oldStatus, RequestStatus newStatus, string? reason)
    {
        TravelRequestId = travelRequest.Id;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Reason = reason;
    }
}
