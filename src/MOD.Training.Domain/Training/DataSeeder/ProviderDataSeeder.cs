using MOD.Training.Training.Finance;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.DataSeeder;

public interface IProviderDataSeeder
{
    Task SeedAsync(DataSeedContext context);
}

public class ProviderDataSeeder(
    IRepository<TrainingProvider, Guid> repo,
    ICurrentTenant currentTenant)
    : ITransientDependency, IProviderDataSeeder
{
    public async Task SeedAsync(DataSeedContext context)
    {
        using (currentTenant.Change(null))
        {
            if (await repo.AnyAsync(x => x.Id == ProviderIds.SANS))
                return;

            await repo.InsertManyAsync(new List<TrainingProvider>
            {
                new(ProviderIds.SANS, "معهد SANS الدولي", "SANS International Institute") { ContactPerson = "John Smith", Email = "contact@sans.org", Phone = "+1-555-0100", Website = "https://www.sans.org", AverageRating = 4.8m, TotalRatings = 127, IsApproved = true, IsActive = true },
                new(ProviderIds.PMI, "معهد PMI الخليج", "PMI Gulf Institute") { ContactPerson = "سعيد المعمري", Email = "info@pmigulf.com", Phone = "+968-2400-1234", Website = "https://www.pmigulf.com", AverageRating = 4.5m, TotalRatings = 89, IsApproved = true, IsActive = true },
                new(ProviderIds.CISCO, "أكاديمية سيسكو", "Cisco Networking Academy") { ContactPerson = "Sarah Johnson", Email = "academy@cisco.com", Phone = "+1-555-0200", Website = "https://www.netacad.com", AverageRating = 4.6m, TotalRatings = 203, IsApproved = true, IsActive = true },
                new(ProviderIds.LocalAcademy, "الأكاديمية العُمانية للتدريب", "Omani Training Academy") { ContactPerson = "فاطمة الحبسية", Email = "admin@ota.om", Phone = "+968-2200-5678", Website = "https://www.ota.om", AverageRating = 3.9m, TotalRatings = 45, IsApproved = false, IsActive = true },
            }, autoSave: true);
        }
    }
}
