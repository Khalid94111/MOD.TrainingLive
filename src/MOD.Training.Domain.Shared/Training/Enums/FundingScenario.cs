namespace MOD.Training.Training.Enums;

public enum FundingScenario
{
    FundingSourceCoversAll    = 1, // No reallocation (no travel items)
    FundingSourceCoversCourse = 2, // Travel items via reallocation
    FinancialItemsCoverAll    = 3  // Full reallocation
}
