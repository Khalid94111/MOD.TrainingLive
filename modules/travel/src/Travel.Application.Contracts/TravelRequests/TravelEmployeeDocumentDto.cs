using System;
using Volo.Abp.Application.Dtos;

namespace Travel.TravelRequests;

public class TravelEmployeeDocumentDto : EntityDto<Guid>
{
    public Guid TravelRequestId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public string Pnr { get; set; } = string.Empty;
    public string TicketFileName { get; set; } = string.Empty;
    public string TicketBlobName { get; set; } = string.Empty;
    public string VisaFileName { get; set; } = string.Empty;
    public string VisaBlobName { get; set; } = string.Empty;
    public string InsuranceFileName { get; set; } = string.Empty;
    public string InsuranceBlobName { get; set; } = string.Empty;
}
