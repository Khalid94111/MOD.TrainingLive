using System;
using Volo.Abp.Domain.Entities;

namespace MOD.Training.Training.Finance;

public class ExchangeRate : Entity<Guid>
{
    public ExchangeRate(Guid id) : base(id)
    {
        
    }
    public ExchangeRate()
    {
        
    }
    public string FromCurrency { get; set; } = "USD";
    public string ToCurrency { get; set; } = "OMR";
    public decimal Rate { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid SetById { get; set; }
    public DateTime SetAt { get; set; } = DateTime.UtcNow;
}
