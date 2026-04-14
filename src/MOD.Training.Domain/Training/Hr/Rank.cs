using System;
using Volo.Abp.Domain.Entities;

namespace MOD.Training.Training.Hr;

/// <summary>
/// Read-only entity from HR module shared DbContext.
/// Represents military/civilian ranks.
/// Mapped with ExcludeFromMigrations.
/// </summary>
public class Rank : Entity<Guid>
{
   
    public Rank(Guid id) : base(id) { }
    
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int SortOrder { get; set; } // Lower = lower rank (Private=1, General=20)
    public string PersonnelType { get; set; } = string.Empty; // "Officer" or "Enlisted"
    public bool IsActive { get; set; }
}
