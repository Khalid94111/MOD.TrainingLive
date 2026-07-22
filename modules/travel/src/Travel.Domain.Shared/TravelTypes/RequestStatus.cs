namespace Travel.TravelTypes;

public enum RequestStatus
{
    Draft = 0,
    PendingApproval = 1,
    Returned = 2,
    Rejected = 3,
    Approved = 4,
    AtTravelOffice = 5,
    VisaCheck = 6,
    InsuranceCheck = 7,
    FlightSelection = 8,
    TicketsBooked = 9,
    DocsUploaded = 10,
    PendingEmployeeConfirm = 11,
    Confirmed = 12,
    CalculatingAllowances = 13,
    Completed = 14,
    Cancelled = 15
}
