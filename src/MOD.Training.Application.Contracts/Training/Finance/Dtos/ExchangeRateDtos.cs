using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Finance.Dtos;

public class ExchangeRateDto : EntityDto<Guid>
{
    public string FromCurrency { get; set; } = default!;
    public string ToCurrency { get; set; } = default!;
    public decimal Rate { get; set; }
    public bool IsActive { get; set; }
    public DateTime SetAt { get; set; }
}

public class CreateExchangeRateDto
{
    public decimal Rate { get; set; }
    public string? Notes { get; set; }
}

public class ExchangeRateGetListInput : PagedAndSortedResultRequestDto
{
}
