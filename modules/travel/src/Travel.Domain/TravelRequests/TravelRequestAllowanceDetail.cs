using System;
using Travel.Allowances;
using Volo.Abp.Domain.Entities;

namespace Travel.TravelRequests;

public class TravelRequestAllowanceDetail : Entity<Guid>
{
    public Guid TravelRequestId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public string EmployeeNumber { get; private set; } = string.Empty;
    public string RankName { get; private set; } = string.Empty;
    public AllowanceCategory Category { get; private set; }
    public decimal DailyRate { get; private set; }
    public int CalculatedDays { get; private set; }
    public decimal AccommodationPaymentPercentage { get; private set; }
    public decimal OverseasTotal { get; private set; }
    public decimal ClothingAmount { get; private set; }
    public decimal DeductionAmount { get; private set; }
    public decimal NetTotal { get; private set; }
    public bool HasMatchingRule { get; private set; }
    public string SegmentsJson { get; private set; } = string.Empty;
    public string ClothingCalculationNote { get; private set; } = string.Empty;
    public DateTime CalculatedAt { get; private set; }

    protected TravelRequestAllowanceDetail()
    {
    }

    public TravelRequestAllowanceDetail(
        Guid id,
        Guid travelRequestId,
        Guid employeeId,
        string employeeName,
        string employeeNumber,
        string rankName,
        AllowanceCategory category,
        decimal dailyRate,
        int calculatedDays,
        decimal accommodationPaymentPercentage,
        decimal overseasTotal,
        decimal clothingAmount,
        decimal deductionAmount,
        bool hasMatchingRule,
        string segmentsJson,
        string clothingCalculationNote,
        DateTime calculatedAt) : base(id)
    {
        TravelRequestId = travelRequestId;
        EmployeeId = employeeId;
        EmployeeName = employeeName;
        EmployeeNumber = employeeNumber;
        RankName = rankName;
        Category = category;
        DailyRate = dailyRate;
        CalculatedDays = calculatedDays;
        AccommodationPaymentPercentage = accommodationPaymentPercentage;
        OverseasTotal = overseasTotal;
        ClothingAmount = clothingAmount;
        DeductionAmount = deductionAmount;
        NetTotal = overseasTotal + clothingAmount - deductionAmount;
        HasMatchingRule = hasMatchingRule;
        SegmentsJson = segmentsJson;
        ClothingCalculationNote = clothingCalculationNote;
        CalculatedAt = calculatedAt;
    }
}
