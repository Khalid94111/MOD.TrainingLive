using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Finance.Dtos;

public class TrainingBudgetDto : EntityDto<Guid>
{
    public int Year { get; set; }
    public BudgetType BudgetType { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal Remaining { get; set; } // Computed: TotalAmount - SpentAmount
    public decimal AlertThreshold { get; set; }
    public bool IsOverThreshold { get; set; } // Computed: SpentPercent > AlertThreshold
    public decimal SpentPercent { get; set; } // Computed
}

public class CreateUpdateTrainingBudgetDto
{
    public int Year { get; set; }
    public BudgetType BudgetType { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal AlertThreshold { get; set; } = 80m;
}

public class TrainingBudgetGetListInput : PagedAndSortedResultRequestDto
{
    public int? Year { get; set; }
}
