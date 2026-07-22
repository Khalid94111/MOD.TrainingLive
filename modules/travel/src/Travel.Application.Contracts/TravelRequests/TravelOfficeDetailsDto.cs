using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Travel.Allowances;

namespace Travel.TravelRequests;

public class TravelOfficeDetailsDto
{
    public bool RequiresVisa { get; set; }
    public decimal VisaCostPerEmployee { get; set; }
    public bool RequiresTravelInsurance { get; set; }
    public decimal TravelInsuranceCostPerEmployee { get; set; }
    public FlightOfferDto Ticket { get; set; } = new();
    public List<TravelFlightOfferInputDto> FlightOffers { get; set; } = new();

    [StringLength(1000)]
    public string TravelOfficeNotes { get; set; } = string.Empty;
}
