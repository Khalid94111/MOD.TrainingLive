using System;
using Travel.Allowances;
using Travel.TravelTypes;
using Volo.Abp.Domain.Entities;

namespace Travel.TravelRequests;

public class TravelRequestEmployee : Entity<Guid>
{
    public Guid TravelRequestId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public string EmployeeNumber { get; private set; } = string.Empty;
    public Guid? RankId { get; private set; }
    public string RankName { get; private set; } = string.Empty;
    public AllowanceCategory Category { get; private set; }
    public decimal DailyAllowanceRate { get; private set; }
    public TicketClass TicketClass { get; private set; }

    protected TravelRequestEmployee()
    {
    }

    public TravelRequestEmployee(
        Guid id,
        Guid travelRequestId,
        Guid employeeId,
        string employeeName,
        string employeeNumber,
        Guid? rankId,
        string rankName,
        AllowanceCategory category,
        decimal dailyAllowanceRate,
        TicketClass ticketClass = TicketClass.Economy)
    {
        Id = id;
        TravelRequestId = travelRequestId;
        EmployeeId = employeeId;
        EmployeeName = employeeName;
        EmployeeNumber = employeeNumber;
        RankId = rankId;
        RankName = rankName;
        Category = category;
        DailyAllowanceRate = dailyAllowanceRate;
        TicketClass = ticketClass;
    }

    public void Update(string employeeName, string employeeNumber, Guid? rankId, string rankName, AllowanceCategory category, decimal dailyAllowanceRate, TicketClass ticketClass = TicketClass.Economy)
    {
        EmployeeName = employeeName;
        EmployeeNumber = employeeNumber;
        RankId = rankId;
        RankName = rankName;
        Category = category;
        DailyAllowanceRate = dailyAllowanceRate;
        TicketClass = ticketClass;
    }
}
