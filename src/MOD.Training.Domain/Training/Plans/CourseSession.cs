using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Plans;

public class CourseSession : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid CourseId { get; set; }
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

    public Course? Course { get; set; }

    protected CourseSession() { }

    public CourseSession(
        Guid id,
        Guid courseId,
        string sessionCode,
        DateTime startDate,
        DateTime endDate,
        int maxSeats) : base(id)
    {
        CourseId = courseId;
        SessionCode = sessionCode;
        StartDate = startDate;
        EndDate = endDate;
        MaxSeats = maxSeats;
        AvailableSeats = maxSeats;
        Status = SessionStatus.Scheduled;
        CompletionStatus = CompletionStatus.NotStarted;
    }
}
