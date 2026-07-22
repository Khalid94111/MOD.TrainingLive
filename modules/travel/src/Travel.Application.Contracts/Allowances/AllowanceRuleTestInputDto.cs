namespace Travel.Allowances;

public class AllowanceRuleTestInputDto
{
    public int Days { get; set; } = 1;
    public decimal DailyRate { get; set; }
    public int EmployeeCount { get; set; } = 1;
    public decimal AccommodationPaymentPercentage { get; set; } = 100m;
}
