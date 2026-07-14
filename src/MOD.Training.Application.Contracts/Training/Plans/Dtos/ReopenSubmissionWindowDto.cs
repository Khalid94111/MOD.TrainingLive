using System;

namespace MOD.Training.Training.Plans.Dtos;

/// <summary>
/// Input for reopening a plan submission window.
/// </summary>
public class ReopenSubmissionWindowDto
{
    public DateTime OpenDate { get; set; }
    public DateTime CloseDate { get; set; }
}
