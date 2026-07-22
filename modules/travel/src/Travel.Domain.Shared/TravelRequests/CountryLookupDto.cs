using System;

namespace Travel.TravelRequests;

public class CountryLookupDto
{
    public Guid Id { get; set; }
    public string ArabicName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
