using System;

namespace Travel.TravelRequests;

public class PayrollElementLookupDto
{
    public Guid Id { get; set; }

    public string ArabicName { get; set; } = string.Empty;

    public string EnglishName { get; set; } = string.Empty;

    public Guid PayrollElementSetId { get; set; }

    public int SegmentCode { get; set; }

    public bool IsActive { get; set; }
}
