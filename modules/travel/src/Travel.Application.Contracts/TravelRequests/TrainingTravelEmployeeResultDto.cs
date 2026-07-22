using System.Collections.Generic;
using System;

namespace Travel.TravelRequests;

public class TrainingTravelEmployeeResultDto
{
    public Guid PersonnelId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string RankName { get; set; } = string.Empty;
    public decimal TicketAmount { get; set; }
    public decimal VisaAmount { get; set; }
    public decimal HealthInsuranceAmount { get; set; }
    public decimal TravelAllowanceAmount { get; set; }
    public decimal ClothingAllowanceAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal AllowanceTotal { get; set; }
    public List<TrainingTravelPaymentDto> Payments { get; set; } = new();
    public decimal TotalAmount { get; set; }
}
