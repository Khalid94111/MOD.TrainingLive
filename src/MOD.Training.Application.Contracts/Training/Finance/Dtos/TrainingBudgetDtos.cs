using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MOD.Training.Training.Enums;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Finance.Dtos;

public class TrainingBudgetDto : EntityDto<Guid>
{
    public int Year { get; set; }
    public Guid FinancialItemId { get; set; }
    public string FinancialItemNameAr { get; set; } = null!;
    public string FinancialItemNameEn { get; set; } = null!;
    public string FinancialItemVoteCode { get; set; } = string.Empty;
    public FinancialItemType? FinancialItemType { get; set; }
    public string BudgetCategoryNameAr { get; set; } = string.Empty;

    // Compatibility aliases used by existing clients.
    public decimal TotalAmount { get; set; }
    public decimal SpentAmount { get; set; }

    public decimal AllocatedAmountOMR { get; set; }
    public decimal GrossSpentAmountOMR { get; set; }
    public decimal RecoveredAmountOMR { get; set; }
    public decimal AmountToRecoverOMR { get; set; }
    public decimal NetSpentAmountOMR { get; set; }
    public decimal Remaining { get; set; }
    public decimal AlertThreshold { get; set; }
    public bool IsOverThreshold { get; set; }
    public bool IsOverBudget { get; set; }
    public decimal SpentPercent { get; set; }
    public bool IsFinancialItemActive { get; set; }
    public List<TrainingBudgetActivityDto> Activities { get; set; } = [];
}

public class TrainingBudgetActivityDto
{
    public string ActivityType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public Guid? TrainingCourseId { get; set; }
    public string CourseNameAr { get; set; } = string.Empty;
    public DateTime ActivityDate { get; set; }
    public decimal AllocatedAmountOMR { get; set; }
    public decimal SpentAmountOMR { get; set; }
    public decimal RecoveredAmountOMR { get; set; }
    public decimal PendingRecoveryAmountOMR { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string? Reference { get; set; }
}

public class UpdateAlertThresholdDto
{
    [Required]
    [Range(0, 100)]
    public decimal AlertThreshold { get; set; }
}

public class TrainingBudgetGetListInput : PagedAndSortedResultRequestDto
{
    public int? Year { get; set; }
    public Guid? FinancialItemId { get; set; }
}
