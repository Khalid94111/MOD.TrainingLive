using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Finance.Dtos;

public class TrainingBudgetDto : EntityDto<Guid>
{
    public int Year { get; set; }
    public Guid FinancialItemId { get; set; }
    public string FinancialItemNameAr { get; set; } = null!;
    public string FinancialItemNameEn { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal Remaining { get; set; } // Computed: TotalAmount - SpentAmount
    public decimal AlertThreshold { get; set; }
    public bool IsOverThreshold { get; set; } // Computed: SpentPercent > AlertThreshold
    public decimal SpentPercent { get; set; } // Computed
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
