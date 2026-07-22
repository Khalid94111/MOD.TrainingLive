using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Travel.Tenants;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Saas.Tenants;

namespace MOD.Training.Tenants;

public class TrainingTenantLookup : ITenantLookup, ITransientDependency
{
    private readonly ITenantRepository _tenantRepository;

    public TrainingTenantLookup(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<List<TenantLookupDto>> GetTenantsAsync()
    {
        var tenants = await _tenantRepository.GetListAsync();

        return tenants
            .Select(MapTenant)
            .OrderBy(x => x.ArabicDescription, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.EnglishDescription, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static TenantLookupDto MapTenant(Tenant tenant)
    {
        var nameAr = GetExtraProperty(tenant, "ArabicDescription", "NameAr", "ArabicName", "TenantNameAr", "DisplayNameAr");
        var nameEn = GetExtraProperty(tenant, "EnglishDescription", "NameEn", "EnglishName", "TenantNameEn", "DisplayNameEn");

        if (string.IsNullOrWhiteSpace(nameAr))
        {
            nameAr = tenant.Name;
        }

        if (string.IsNullOrWhiteSpace(nameEn))
        {
            nameEn = tenant.Name;
        }

        return new TenantLookupDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            ArabicDescription = nameAr,
            EnglishDescription = nameEn
        };
    }

    private static string GetExtraProperty(Tenant tenant, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!tenant.ExtraProperties.TryGetValue(key, out var value) || value is null)
            {
                continue;
            }

            var textValue = value.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(textValue))
            {
                return textValue;
            }
        }

        return string.Empty;
    }
}
