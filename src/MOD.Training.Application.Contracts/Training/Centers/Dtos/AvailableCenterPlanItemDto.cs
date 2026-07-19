using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Centers.Dtos;

public class AvailableCenterPlanItemDto : EntityDto<Guid>
{
    public Guid CenterPlanId { get; set; }
    public Guid CenterId { get; set; }
    public string CenterName { get; set; } = string.Empty;
    public Guid? SourceTenantId { get; set; }
    public string? SourceTenantName { get; set; }

    public Guid TenantCourseId { get; set; }
    public Guid CatalogCourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;

    public DateTime EstimatedStartDate { get; set; }
    public DateTime EstimatedEndDate { get; set; }
    public int DurationWeeks { get; set; }

    public int Capacity { get; set; }
    public int ReservedSeats { get; set; }
    public int RemainingSeats { get; set; }

    public BeneficiaryType BeneficiaryType { get; set; }
    public List<Guid> EligibleUnitIds { get; set; } = [];
}
