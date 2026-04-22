using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Finance.Dtos;

public class FinancialItemRankAmountDto : FullAuditedEntityDto<Guid>
{
    public Guid FinancialItemId { get; set; }
    public Guid RankId { get; set; }
    public string RankNameAr { get; set; } = string.Empty;
    public string RankNameEn { get; set; } = string.Empty;
    public decimal AmountOMR { get; set; }
}

public class CreateUpdateFinancialItemRankAmountDto
{
    public Guid FinancialItemId { get; set; }
    public Guid RankId { get; set; }
    public decimal AmountOMR { get; set; }
}

public class FinancialItemRankAmountGetListInput : PagedAndSortedResultRequestDto
{
    public Guid FinancialItemId { get; set; }
}
