using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Plans.Dtos;

// Phase 4C-α (v4.10.0) — one row in CourseSessionDetailDto.Nominations.
// EmployeeName / RankName are resolved server-side via HR Employee + Rank lookups.
public class SessionNominationDto : EntityDto<Guid>
{
    public Guid SessionId { get; set; }

    public Guid EmployeeId { get; set; }
    public Guid OriginalEmployeeId { get; set; }
    public bool WasSubstituted { get; set; }
    public string? SubstitutionReason { get; set; }

    public Guid RankId { get; set; }
    public string? RankNameAr { get; set; }

    public string? EmployeeNameAr { get; set; }
    public string? EmployeeNameEn { get; set; }
    public string? OriginalEmployeeNameAr { get; set; }

    public DateTime CreationTime { get; set; }
}
