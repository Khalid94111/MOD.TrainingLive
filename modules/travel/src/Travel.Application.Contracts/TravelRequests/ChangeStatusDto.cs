using System;
using System.ComponentModel.DataAnnotations;
using Travel.TravelTypes;

namespace Travel.TravelRequests;

public class ChangeStatusDto
{
    [Required]
    public RequestStatus NewStatus { get; set; }

    public string? Reason { get; set; }

    /// <summary>
    /// Required when completing a travel request that exceeds 60 days.
    /// The selected payroll element will be used to create overseas payment tasks for the extra days.
    /// </summary>
    public Guid? PayrollElementId { get; set; }
}
