using System;
using Travel.Allowances;

namespace Travel.TravelRequests;

public class TravelEmployeeLookupResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public Guid? RankId { get; set; }
    public string RankName { get; set; } = string.Empty;
    public AllowanceCategory Category { get; set; } = AllowanceCategory.A;
}
