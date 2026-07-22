namespace Travel.Allowances;

public class AllowanceCalculationSegment
{
    public int FromDay { get; set; }
    public int ToDay { get; set; }
    public int Days { get; set; }
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }

    public AllowanceCalculationSegment()
    {
    }

    public AllowanceCalculationSegment(int fromDay, int toDay, int days, decimal percentage, decimal amount)
    {
        FromDay = fromDay;
        ToDay = toDay;
        Days = days;
        Percentage = percentage;
        Amount = amount;
    }
}
