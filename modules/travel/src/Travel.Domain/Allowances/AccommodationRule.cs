using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Travel.Allowances;

public class AccommodationRule : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public decimal PaymentPercentage { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }

    protected AccommodationRule()
    {
    }

    public AccommodationRule(
        Guid id,
        string name,
        decimal paymentPercentage,
        int priority,
        bool isActive) : base(id)
    {
        Update(name, paymentPercentage, priority, isActive);
    }

    public void Update(
        string name,
        decimal paymentPercentage,
        int priority,
        bool isActive)
    {
        Check.NotNullOrWhiteSpace(name, nameof(name));
        if (name.Length > 256)
        {
            throw new BusinessException(TravelErrorCodes.NameTooLong);
        }

        if (paymentPercentage < 0 || paymentPercentage > 100)
        {
            throw new BusinessException(TravelErrorCodes.InvalidAllowanceSegment);
        }

        Name = name;
        PaymentPercentage = paymentPercentage;
        Priority = priority;
        IsActive = isActive;
    }
}
