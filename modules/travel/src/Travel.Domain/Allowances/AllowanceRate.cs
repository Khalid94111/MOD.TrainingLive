using System;
using Travel.TravelTypes;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Travel.Allowances;

public class AllowanceRate : FullAuditedAggregateRoot<Guid>
{
    public Guid RankId { get; private set; }
    public AllowanceCategory Category { get; private set; }
    public AllowanceType AllowanceType { get; private set; }
    public decimal Amount { get; private set; }
    public decimal? AnnualPartialAmount { get; private set; }
    public TicketClass TicketClass { get; private set; }
    public bool IsActive { get; private set; }

    protected AllowanceRate()
    {
    }

    public AllowanceRate(
        Guid id,
        Guid rankId,
        AllowanceCategory category,
        AllowanceType allowanceType,
        decimal amount,
        TicketClass ticketClass = TicketClass.Economy,
        decimal? annualPartialAmount = null,
        bool isActive = true) : base(id)
    {
        RankId = rankId;
        Category = category;
        AllowanceType = allowanceType;
        SetAmount(amount);
        SetAnnualPartialAmount(annualPartialAmount);
        TicketClass = ticketClass;
        IsActive = isActive;
    }

    public void Update(Guid rankId, AllowanceCategory category, decimal amount, TicketClass ticketClass, decimal? annualPartialAmount, bool isActive)
    {
        RankId = rankId;
        Category = category;
        SetAmount(amount);
        SetAnnualPartialAmount(annualPartialAmount);
        TicketClass = ticketClass;
        IsActive = isActive;
    }

    private void SetAmount(decimal amount)
    {
        if (amount < 0)
            throw new BusinessException(TravelErrorCodes.NegativeAmount);
        Amount = amount;
    }

    private void SetAnnualPartialAmount(decimal? annualPartialAmount)
    {
        if (annualPartialAmount < 0)
            throw new BusinessException(TravelErrorCodes.NegativeAmount);

        AnnualPartialAmount = annualPartialAmount;
    }
}
