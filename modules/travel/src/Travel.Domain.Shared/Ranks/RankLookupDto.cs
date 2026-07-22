using System;
using Travel.Allowances;

namespace Travel.Ranks;

public class RankLookupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AllowanceCategory Category { get; set; }
    public int Level { get; set; }
}
