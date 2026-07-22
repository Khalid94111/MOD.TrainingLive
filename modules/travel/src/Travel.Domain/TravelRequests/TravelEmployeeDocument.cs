using System;
using Volo.Abp.Domain.Entities;

namespace Travel.TravelRequests;

public class TravelEmployeeDocument : Entity<Guid>
{
    public Guid TravelRequestId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public string EmployeeNumber { get; private set; } = string.Empty;
    public string TicketNumber { get; private set; } = string.Empty;
    public string Pnr { get; private set; } = string.Empty;
    public string TicketFileName { get; private set; } = string.Empty;
    public string TicketBlobName { get; private set; } = string.Empty;
    public string VisaFileName { get; private set; } = string.Empty;
    public string VisaBlobName { get; private set; } = string.Empty;
    public string InsuranceFileName { get; private set; } = string.Empty;
    public string InsuranceBlobName { get; private set; } = string.Empty;

    protected TravelEmployeeDocument()
    {
    }

    public TravelEmployeeDocument(
        Guid id,
        Guid travelRequestId,
        Guid employeeId,
        string employeeName,
        string employeeNumber,
        string ticketNumber,
        string pnr,
        string ticketFileName,
        string ticketBlobName,
        string visaFileName,
        string visaBlobName,
        string insuranceFileName,
        string insuranceBlobName) : base(id)
    {
        TravelRequestId = travelRequestId;
        EmployeeId = employeeId;
        EmployeeName = employeeName ?? string.Empty;
        EmployeeNumber = employeeNumber ?? string.Empty;
        TicketNumber = ticketNumber ?? string.Empty;
        Pnr = pnr ?? string.Empty;
        TicketFileName = ticketFileName ?? string.Empty;
        TicketBlobName = ticketBlobName ?? string.Empty;
        VisaFileName = visaFileName ?? string.Empty;
        VisaBlobName = visaBlobName ?? string.Empty;
        InsuranceFileName = insuranceFileName ?? string.Empty;
        InsuranceBlobName = insuranceBlobName ?? string.Empty;
    }
}
