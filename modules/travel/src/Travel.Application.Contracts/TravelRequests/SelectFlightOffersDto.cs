using System;
using System.Collections.Generic;
using Travel.TravelTypes;

namespace Travel.TravelRequests;

public class SelectFlightOffersDto
{
    public Guid DepartureFlightOfferId { get; set; }
    public Guid ReturnFlightOfferId { get; set; }
    public List<TicketClassFlightSelectionDto> Selections { get; set; } = new();
}

public class TicketClassFlightSelectionDto
{
    public TicketClass TicketClass { get; set; } = TicketClass.Economy;
    public Guid DepartureFlightOfferId { get; set; }
    public Guid ReturnFlightOfferId { get; set; }
}
