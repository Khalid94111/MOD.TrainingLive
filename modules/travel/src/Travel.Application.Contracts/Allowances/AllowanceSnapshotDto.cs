using System;

namespace Travel.Allowances;

public class AllowanceSnapshotDto
{
    public decimal OverseasTotal { get; set; }
    public decimal ClothingTotal { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal NetTotal { get; set; }
    public DateTime CalculatedAt { get; set; }
}
