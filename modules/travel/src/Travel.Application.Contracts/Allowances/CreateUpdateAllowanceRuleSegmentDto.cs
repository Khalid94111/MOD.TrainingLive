using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Travel.Allowances;

public class CreateUpdateAllowanceRuleSegmentDto : IValidatableObject
{
    [Range(1, 9999, ErrorMessage = "FromDay must be between 1 and 9999.")]
    public int FromDay { get; set; }

    [Range(1, 9999, ErrorMessage = "ToDay must be between 1 and 9999.")]
    public int ToDay { get; set; }

    [Range(0, 100)]
    public decimal Percentage { get; set; }

    [Range(1, 9999)]
    public int? AppliesWhenTotalDaysFrom { get; set; }

    [Range(1, 9999)]
    public int? AppliesWhenTotalDaysTo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ToDay < FromDay)
            yield return new ValidationResult(
                "ToDay must be greater than or equal to FromDay.",
                new[] { nameof(ToDay) });

        if (AppliesWhenTotalDaysFrom.HasValue && AppliesWhenTotalDaysTo.HasValue
            && AppliesWhenTotalDaysTo.Value < AppliesWhenTotalDaysFrom.Value)
            yield return new ValidationResult(
                "AppliesWhenTotalDaysTo must be >= AppliesWhenTotalDaysFrom.",
                new[] { nameof(AppliesWhenTotalDaysTo) });
    }
}
