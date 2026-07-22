using System;
using Travel.TravelTypes;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace Travel.TravelRequests;

public class TravelFlightOffer : Entity<Guid>
{
    public Guid TravelRequestId { get; private set; }
    public FlightDirection Direction { get; private set; }
    public TicketClass TicketClass { get; private set; }
    public string Airline { get; private set; } = string.Empty;
    public string FlightNumber { get; private set; } = string.Empty;
    public DateTime DepartureTime { get; private set; }
    public DateTime ArrivalTime { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "OMR";
    public string Duration { get; private set; } = string.Empty;
    public bool IsSelected { get; private set; }

    protected TravelFlightOffer()
    {
    }

    public TravelFlightOffer(
        Guid id,
        Guid travelRequestId,
        FlightDirection direction,
        TicketClass ticketClass,
        string airline,
        string flightNumber,
        DateTime departureTime,
        DateTime arrivalTime,
        decimal price,
        string currency,
        string duration) : base(id)
    {
        Update(travelRequestId, direction, ticketClass, airline, flightNumber, departureTime, arrivalTime, price, currency, duration);
    }

    public void Update(
        Guid travelRequestId,
        FlightDirection direction,
        TicketClass ticketClass,
        string airline,
        string flightNumber,
        DateTime departureTime,
        DateTime arrivalTime,
        decimal price,
        string currency,
        string duration)
    {
        if (price < 0)
        {
            throw new BusinessException(TravelErrorCodes.NegativeAmount);
        }

        TravelRequestId = travelRequestId;
        Direction = direction;
        TicketClass = ticketClass;
        Airline = airline ?? string.Empty;
        FlightNumber = flightNumber ?? string.Empty;
        DepartureTime = departureTime;
        ArrivalTime = arrivalTime;
        Price = price;
        Currency = string.IsNullOrWhiteSpace(currency) ? "OMR" : currency;
        Duration = duration ?? string.Empty;
    }

    public void Select()
    {
        IsSelected = true;
    }

    public void Unselect()
    {
        IsSelected = false;
    }
}
