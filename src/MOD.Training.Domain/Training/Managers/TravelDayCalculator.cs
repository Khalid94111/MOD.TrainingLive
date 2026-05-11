using MOD.Training.Training.Execution;
using System;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace MOD.Training.Training.Managers;

/// <summary>
/// Server-side computation of travel days for a TravelInstruction.
/// Default = inclusive day count from departure (leaving Oman) to arrival back.
/// Staff can override via <see cref="TravelInstruction.OverrideTravelDays"/>.
/// </summary>
public class TravelDayCalculator : DomainService
{
    public int CalculateDays(DateTime departureDate, DateTime arrivalBackDate)
    {
        if (arrivalBackDate.Date < departureDate.Date)
            throw new BusinessException("Training:TravelInstruction:InvalidDateOrder");

        return (int)(arrivalBackDate.Date - departureDate.Date).TotalDays + 1;
    }

    /// <summary>
    /// Returns days actually used for allowance calculation.
    /// Override wins if set, otherwise the computed default.
    /// Phase 4B-β consumes this from <c>TravelAllowancePaymentAppService</c>.
    /// </summary>
    public int GetEffectiveTravelDays(TravelInstruction instruction)
    {
        return instruction.OverrideTravelDays ?? instruction.CalculatedTravelDays;
    }
}
