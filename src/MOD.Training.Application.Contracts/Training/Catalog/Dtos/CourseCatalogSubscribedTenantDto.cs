using System;

namespace MOD.Training.Training.Catalog.Dtos;

/// <summary>
/// Represents a tenant that has added a specific catalog course to its own courses.
/// </summary>
public class CourseCatalogSubscribedTenantDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = null!;
}
