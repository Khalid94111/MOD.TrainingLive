namespace MOD.Training.Training.Enums;

public enum FundingScenario
{
    FundingSourceCoversAll    = 1, // Course + Travel use the entered funding-source vote code
    FundingSourceCoversCourse = 2, // Course uses entered code; Travel uses configured item codes
    FinancialItemsCoverAll    = 3  // Legacy value; not selectable for new casual courses
}
