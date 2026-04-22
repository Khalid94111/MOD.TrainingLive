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

    // CHG-02 + CHG-07
    public decimal DefaultAmountOMR { get; set; }
    public bool IsPerDay { get; set; }
    public bool IsPerNominee { get; set; }
    public int ExtraDaysBefore { get; set; }
    public int ExtraDaysAfter { get; set; }
}

public class CreateUpdateFinancialItemDto
{
    public Guid? ParentId { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string VoteCode { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    // CHG-02 + CHG-07
    public decimal DefaultAmountOMR { get; set; }
    public bool IsPerDay { get; set; }
    public bool IsPerNominee { get; set; }
    public int ExtraDaysBefore { get; set; }
    public int ExtraDaysAfter { get; set; }
}

public class FinancialItemGetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public bool? IsActive { get; set; }
}
