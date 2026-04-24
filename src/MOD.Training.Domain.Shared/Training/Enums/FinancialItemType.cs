namespace MOD.Training.Training.Enums;

/// <summary>
/// Classifies a leaf-level <c>FinancialItem</c> for scenario-aware source derivation.
/// Parent (grouping) rows leave <c>FinancialItem.ItemType</c> as NULL; billable leaves
/// carry a concrete value. The <c>FundingScenarioSourceResolver</c> treats any non
/// <c>CourseCost</c> (or NULL) leaf as a travel/ancillary item under scenario 2.
/// </summary>
public enum FinancialItemType
{
    Other      = 0,
    CourseCost = 1,
    Ticket     = 2,
    Insurance  = 3,
    Visa       = 4,
    Allowance  = 5,
    Clothing   = 6,
}
