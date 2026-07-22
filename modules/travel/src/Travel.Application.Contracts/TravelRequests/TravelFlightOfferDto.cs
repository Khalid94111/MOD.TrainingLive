using System;
using Travel.TravelTypes;
using Volo.Abp.Application.Dtos;

namespace Travel.TravelRequests;

public class TravelFlightOfferDto : EntityDto<Guid>
{
    public Guid TravelRequestId { get; set; }
    public FlightDirection Direction { get; set; }
    public TicketClass TicketClass { get; set; } = TicketClass.Economy;
    public string Airline { get; set; } = string.Empty;
    public string FlightNumber { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "OMR";
    public string Duration { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
