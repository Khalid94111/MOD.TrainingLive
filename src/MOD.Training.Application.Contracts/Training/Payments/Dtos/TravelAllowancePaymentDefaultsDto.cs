using MOD.Training.Training.Enums;

namespace MOD.Training.Training.Payments.Dtos;

/// <summary>
/// Read-only preview of the default values that <see cref="ITravelAllowancePaymentAppService.CreateAsync"/>
/// would apply for a (casual course, nomination) pair — without persisting anything.
/// Frontend uses this to pre-fill the New-Payment dialog the moment a nominee is selected,
/// so Finance can review/adjust before clicking Save Draft.
///
/// Values are sourced from the casual course's financial breakdown (per-rank rows entered
/// during the review stage) and respect the parent TravelInstruction toggles for Insurance / Visa.
/// </summary>
public class TravelAllowancePaymentDefaultsDto
{
    /// <summary>Per-day rate for the nominee's rank, taken from the Allowance financial item's RatePerUnitOMR.</summary>
    public decimal DailyAllowanceRateOMR { get; set; }

    /// <summary>EffectiveTravelDays × DailyAllowanceRateOMR. Backend recomputes on Save, this is purely a preview.</summary>
    public decimal TravelAllowanceOMR { get; set; }

    public decimal TicketAmountOMR { get; set; }
    public decimal ClothingAllowanceOMR { get; set; }
    public decimal InsuranceOMR { get; set; }
    public decimal VisaFeesOMR { get; set; }

    /// <summary>Days surfaced by TravelDayCalculator on the issued TravelInstruction.</summary>
    public int EffectiveTravelDays { get; set; }

    public PersonnelType PersonnelType { get; set; }
}
