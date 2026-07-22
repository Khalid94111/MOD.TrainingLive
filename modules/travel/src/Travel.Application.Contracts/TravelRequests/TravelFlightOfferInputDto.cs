using System;
using System.ComponentModel.DataAnnotations;
using Travel.TravelTypes;

namespace Travel.TravelRequests;

public class TravelFlightOfferInputDto
{
    public FlightDirection Direction { get; set; }
    public TicketClass TicketClass { get; set; } = TicketClass.Economy;

    [Required]
    [StringLength(128)]
    public string Airline { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string FlightNumber { get; set; } = string.Empty;

    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public decimal Price { get; set; }

    [StringLength(10)]
    public string Currency { get; set; } = "OMR";

    [StringLength(64)]
    public string Duration { get; set; } = string.Empty;
}
