using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Travel.Allowances;

namespace Travel.TravelRequests;

public class AssignFlightsDto
{
    [Required]
    public List<FlightOfferDto> Offers { get; set; } = new();
}
