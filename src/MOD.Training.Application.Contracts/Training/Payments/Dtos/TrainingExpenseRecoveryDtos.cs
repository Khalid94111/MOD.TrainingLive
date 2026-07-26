using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MOD.Training.Training.Enums;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Payments.Dtos;

public class TrainingExpenseRecoveryDto : EntityDto<Guid>
{
    public Guid CasualCourseId { get; set; }
    public string CasualCourseNameAr { get; set; } = string.Empty;
    public Guid TravelRequestId { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string Currency { get; set; } = "OMR";
    public decimal TotalAmountOMR { get; set; }
    public decimal SettledAmountOMR { get; set; }
    public decimal RemainingAmountOMR { get; set; }
    public TrainingExpenseRecoveryStatus Status { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedById { get; set; }
    public string? ReviewedByName { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreationTime { get; set; }
    public List<TrainingExpenseRecoveryItemDto> Items { get; set; } = [];
}

public class TrainingExpenseRecoveryItemDto : EntityDto<Guid>
{
    public Guid? FinancialItemId { get; set; }
    public string? FinancialItemNameAr { get; set; }
    public string ExpenseTypeCode { get; set; } = string.Empty;
    public string FundingSourceVoteCode { get; set; } = string.Empty;
    public decimal AmountOMR { get; set; }
    public bool IsSettled { get; set; }
    public decimal SettledAmountOMR { get; set; }
    public decimal RemainingAmountOMR { get; set; }
    public DateTime? SettledAt { get; set; }
    public Guid? SettledById { get; set; }
    public string? SettledByName { get; set; }
    public string? SettlementReference { get; set; }
    public string? SettlementNote { get; set; }
}

public class TrainingExpenseRecoveryGetListInput : PagedAndSortedResultRequestDto
{
    public Guid? CasualCourseId { get; set; }
    public TrainingExpenseRecoveryStatus? Status { get; set; }
    public string? Search { get; set; }
}

public class MarkTrainingExpenseRecoveryReviewedDto
{
    public string? ReviewNote { get; set; }
}

public class MarkTrainingExpenseRecoverySettledDto
{
    [Required]
    [StringLength(128)]
    public string SettlementReference { get; set; } = string.Empty;
    public string? SettlementNote { get; set; }
}
