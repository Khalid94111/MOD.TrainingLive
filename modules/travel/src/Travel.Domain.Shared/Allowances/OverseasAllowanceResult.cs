namespace Travel.Allowances;

public class OverseasAllowanceResult
{
    public int Days { get; set; }
    public decimal DailyRate { get; set; }
    public decimal TotalPerEmployee { get; set; }
    public int EmployeeCount { get; set; }
    public decimal GrandTotal { get; set; }
}
