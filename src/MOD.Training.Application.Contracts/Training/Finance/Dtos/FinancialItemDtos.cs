using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Finance.Dtos;

public class FinancialItemDto : EntityDto<Guid>
{
    public Guid? ParentId { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string VoteCode { get; set; } = default!;
    public bool IsGeneral { get; set; }
    public bool IsActive { get; set; }
}

public class CreateUpdateFinancialItemDto
{
    public Guid? ParentId { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string VoteCode { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

public class FinancialItemGetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public bool? IsActive { get; set; }
}
