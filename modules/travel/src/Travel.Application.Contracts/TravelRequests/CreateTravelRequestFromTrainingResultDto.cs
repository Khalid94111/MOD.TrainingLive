using System;
using System.Collections.Generic;
using Travel.TravelTypes;

namespace Travel.TravelRequests;

public class CreateTravelRequestFromTrainingResultDto
{
    public Guid TravelRequestId { get; set; }
    public RequestStatus Status { get; set; }
    public bool AlreadyExists { get; set; }
    public List<string> MissingEmployeeNumbers { get; set; } = new();
    public string WarningMessage { get; set; } = string.Empty;
}
