using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Centers.Dtos;

public class CenterPlanNominationDto : EntityDto<Guid>
{
    public Guid CenterPlanId { get; set; }
    public string CenterName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public DateTime EstimatedStartDate { get; set; }
    public DateTime EstimatedEndDate { get; set; }
    public int Capacity { get; set; }
    public int ReservedSeats { get; set; }
    public int RemainingSeats { get; set; }
    public List<PlanItemBookingDto> Bookings { get; set; } = [];
}

public class PlanItemBookingDto
{
    public Guid TrainingPlanItemId { get; set; }
    public Guid? UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public int NomineeCount { get; set; }
    public List<CenterPlanBookingNomineeDto> Nominees { get; set; } = [];
}

public class CenterPlanBookingNomineeDto
{
    public Guid NominationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string ServiceNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? RankName { get; set; }
    public string? UnitName { get; set; }
}
