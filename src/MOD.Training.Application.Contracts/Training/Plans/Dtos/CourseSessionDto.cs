using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Plans.Dtos;

public class CourseSessionDto : FullAuditedEntityDto<Guid>
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty; // Resolved
    public string SessionCode { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public int MaxSeats { get; set; }
    public int AvailableSeats { get; set; }
    public decimal? Cost { get; set; }
    public SessionStatus Status { get; set; }
    public CompletionStatus CompletionStatus { get; set; }
    public DateTime? CompletedAt { get; set; }
}
