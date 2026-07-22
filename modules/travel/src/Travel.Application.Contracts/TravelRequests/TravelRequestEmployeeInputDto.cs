using System;
using System.ComponentModel.DataAnnotations;
using Travel.Allowances;
using Travel.TravelTypes;

namespace Travel.TravelRequests;

public class TravelRequestEmployeeInputDto
{
    public Guid EmployeeId { get; set; }

    [Required]
    [StringLength(200)]
    public string EmployeeName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string EmployeeNumber { get; set; } = string.Empty;

    public Guid? RankId { get; set; }
    public string RankName { get; set; } = string.Empty;
    public AllowanceCategory Category { get; set; } = AllowanceCategory.A;
    public decimal DailyAllowanceRate { get; set; }
    public TicketClass TicketClass { get; set; } = TicketClass.Economy;
}
