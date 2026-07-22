using System;
using System.ComponentModel.DataAnnotations;

namespace Travel.TravelRequests;

public class TravelEmployeeDocumentInputDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string TicketNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string Pnr { get; set; } = string.Empty;

    [StringLength(255)]
    public string TicketFileName { get; set; } = string.Empty;

    [StringLength(255)]
    public string TicketBlobName { get; set; } = string.Empty;

    [StringLength(255)]
    public string VisaFileName { get; set; } = string.Empty;

    [StringLength(255)]
    public string VisaBlobName { get; set; } = string.Empty;

    [StringLength(255)]
    public string InsuranceFileName { get; set; } = string.Empty;

    [StringLength(255)]
    public string InsuranceBlobName { get; set; } = string.Empty;
}
