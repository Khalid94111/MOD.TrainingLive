using System;

namespace Travel.Tenants;

public class TenantLookupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ArabicDescription { get; set; } = string.Empty;
    public string EnglishDescription { get; set; } = string.Empty;
}
