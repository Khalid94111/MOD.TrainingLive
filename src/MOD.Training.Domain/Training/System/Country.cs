using System;
using Volo.Abp.Domain.Entities;

namespace MOD.Training.Training.System;

/// <summary>
/// System lookup: countries used for course locations and travel destinations.
/// </summary>
public class Country : Entity<Guid>
{
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string Code { get; set; } = null!;  // ISO 3166-1 alpha-3
    public bool IsActive { get; set; } = true;
}
