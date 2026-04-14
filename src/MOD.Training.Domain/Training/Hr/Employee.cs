using System;
using Volo.Abp.Domain.Entities;

namespace MOD.Training.Training.Hr;

/// <summary>
/// Read-only entity from HR module shared DbContext.
/// Core employee data needed by GTMS for nominations, condition checks, and unit resolution.
/// Mapped with ExcludeFromMigrations.
/// </summary>
public class Employee : Entity<Guid>
{
    public Employee(Guid id) :  base(id)
    {
        
    }
    public Guid? TenantId { get; set; }
    public Guid UserId { get; set; }         // FK → AbpUsers
    public Guid MainUnitId { get; set; }      // FK → AbpOrganizationUnits
    public Guid RankId { get; set; }          // FK → Ranks
    public string ServiceNumber { get; set; } = string.Empty;
    public string FullNameAr { get; set; } = string.Empty;
    public string FullNameEn { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public DateTime? JoinDate { get; set; }    // For ServiceYears calculation
    public string? Education { get; set; }     // "HighSchool", "Diploma", "Bachelor", "Master", "PhD"
    public string? Nationality { get; set; }
    public bool MedicalFitness { get; set; }
    public string? SecurityClearance { get; set; }
    public string? LanguageLevel { get; set; } // JSON: {"English":"B2","French":"A1"}
    public bool IsActive { get; set; }

    public Rank? Rank { get; set; }
}
