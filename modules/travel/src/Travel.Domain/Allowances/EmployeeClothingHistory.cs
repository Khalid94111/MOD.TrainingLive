using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Travel.Allowances;

public class EmployeeClothingHistory : FullAuditedAggregateRoot<Guid>
{
    public Guid EmployeeId { get; private set; }
    public Guid? TravelRequestId { get; private set; }
    public Guid? ClothingAllowanceRuleId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaidAt { get; private set; }
    public bool IsFullPayment { get; private set; }

    protected EmployeeClothingHistory()
    {
    }

    public EmployeeClothingHistory(
        Guid id,
        Guid employeeId,
        Guid? travelRequestId,
        decimal amount,
        DateTime paidAt,
        bool isFullPayment,
        Guid? clothingAllowanceRuleId = null) : base(id)
    {
        EmployeeId = employeeId;
        TravelRequestId = travelRequestId;
        ClothingAllowanceRuleId = clothingAllowanceRuleId;
        Amount = amount;
        PaidAt = paidAt;
        IsFullPayment = isFullPayment;
    }
}
