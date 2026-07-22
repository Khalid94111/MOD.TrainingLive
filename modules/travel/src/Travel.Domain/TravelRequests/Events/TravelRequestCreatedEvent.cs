using System;
using Volo.Abp.Domain.Entities.Events;

namespace Travel.TravelRequests.Events;

public class TravelRequestCreatedEvent : EntityCreatedEventData<TravelRequest>
{
    public TravelRequestCreatedEvent(TravelRequest entity) : base(entity)
    {
    }
}
