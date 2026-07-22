using System.Collections.Generic;

namespace Travel.TravelRequests;

public class ConfirmTicketBookingDto
{
    public List<TravelEmployeeDocumentInputDto> EmployeeDocuments { get; set; } = new();
}
