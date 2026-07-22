namespace Travel.Allowances;

public class ClothingAllowanceResult
{
    public int YearsOfService { get; set; }
    public decimal Amount { get; set; }
    public bool Eligible { get; set; }
    public int EmployeeCount { get; set; }
    public decimal GrandTotal { get; set; }
}
