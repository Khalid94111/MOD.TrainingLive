using System;

namespace MOD.Training.Training.Finance.Dtos;

/// <summary>
/// Active financial item for dropdown selection (flat list).
/// </summary>
public class FinancialItemSubItemDto
{
    public Guid Id { get; set; }
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string Code { get; set; } = null!;
}
