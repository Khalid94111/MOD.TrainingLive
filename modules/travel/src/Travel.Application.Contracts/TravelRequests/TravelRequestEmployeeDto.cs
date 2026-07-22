using System;
using Travel.Allowances;
using Travel.TravelTypes;
using Volo.Abp.Application.Dtos;

namespace Travel.TravelRequests;

public class TravelRequestEmployeeDto : EntityDto<Guid>
{
    public Guid TravelRequestId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public Guid? RankId { get; set; }
    public string RankName { get; set; } = string.Empty;
    public AllowanceCategory Category { get; set; }
    public decimal DailyAllowanceRate { get; set; }
    public TicketClass TicketClass { get; set; } = TicketClass.Economy;
}
