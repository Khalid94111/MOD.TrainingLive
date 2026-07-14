using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace MOD.Training.Training.Managers;

/// <summary>
/// Single source of truth for mapping (<see cref="FundingScenario"/>, <see cref="FinancialItem.ItemType"/>)
/// → <see cref="FinancialAmountSource"/> on a CasualCourseFinancialItem row. Used by
/// AssignScenarioAsync so the rule lives in one place.
///
/// Under scenario 2 a leaf whose ItemType is NULL is treated as "not course cost" —
/// the safe default pushes the cost onto the training budget rather than charging
/// it to the funding source.
/// </summary>
public class FundingScenarioSourceResolver : DomainService
{
    public FinancialAmountSource Resolve(FundingScenario scenario, FinancialItem item)
    {
        return scenario switch
        {
            FundingScenario.FundingSourceCoversAll    => FinancialAmountSource.FundingSource,
            FundingScenario.FinancialItemsCoverAll    => FinancialAmountSource.FinancialItem,
            FundingScenario.FundingSourceCoversCourse => item.ItemType == FinancialItemType.CourseCost
                ? FinancialAmountSource.FundingSource
                : FinancialAmountSource.FinancialItem,
            _ => throw new BusinessException("Training:CasualCourse:UnknownFundingScenario"),
        };
    }
}
