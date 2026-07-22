using System;
using System.Collections.Generic;
using Travel.TravelTypes;
using Volo.Abp.Application.Dtos;

namespace Travel.TravelRequests;

public class GetTravelRequestListInput : PagedAndSortedResultRequestDto
{
    public Guid? TravelTypeDefinitionId { get; set; }
    public RequestStatus? Status { get; set; }
    public List<RequestStatus> Statuses { get; set; } = new();
    public string? SearchText { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public string? Department { get; set; }
}
