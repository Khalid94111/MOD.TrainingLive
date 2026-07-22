using System;

namespace Travel.Allowances;

public class FlightOfferDto
{
    public string Airline { get; set; } = string.Empty;
    public string FlightNumber { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
}
