using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;
using Volo.Saas.Tenants;

namespace MOD.Training.Training.DataSeeder;

/// <summary>
/// GTMS Master Data Seeder — orchestrates all sub-seeders in order.
/// Each sub-seeder runs in its own UnitOfWork to ensure data is committed
/// before the next seeder references it (FK dependencies).
/// </summary>
public class GtmsDataSeeder(
    IRankDataSeeder rankDataSeeder,
    ICourseFieldDataSeeder courseFieldDataSeeder,
    ICourseCatalogDataSeeder courseCatalogDataSeeder,
    IProviderDataSeeder providerDataSeeder,
    IGeographicalLocationDataSeeder geographicalLocationDataSeeder,
    ITenantRepository tenantRepository,
    ITenantDataSeeder tenantDataSeeder,
    IUnitOfWorkManager unitOfWorkManager,
    ILogger<GtmsDataSeeder> logger)
    : IDataSeedContributor, ITransientDependency
{


    public async Task SeedAsync(DataSeedContext context)
    {
        logger.LogInformation("🚀 GTMS Comprehensive Data Seeder — Starting...");

        // Phase 0: Supra-tenant data (no tenant context)
        //await ExecuteInUowAsync("Ranks", () => rankDataSeeder.SeedAsync(context));
        //await ExecuteInUowAsync("CourseFields", () => courseFieldDataSeeder.SeedAsync(context));
        //await ExecuteInUowAsync("CourseCatalog", () => courseCatalogDataSeeder.SeedAsync(context));
        //await ExecuteInUowAsync("Providers (supra)", () => providerDataSeeder.SeedAsync(context));

        // Phase 4B-α — geographical locations for Country/City cascade (idempotent).
        await ExecuteInUowAsync("GeographicalLocations", () => geographicalLocationDataSeeder.SeedAsync(context));

        //var tenantList =await tenantRepository.GetListAsync();
        //tenantList.ForEach(async tenant => {
        

        //    await ExecuteInUowAsync("Tenant: Ground Forces", () => tenantDataSeeder.SeedTenantAsync(context, tenant, tenant.NormalizedName, TenantScenario.FullApproved));

        //});
        // Phase 1-3: Per-tenant data (3 tenants, different scenarios)
        //await ExecuteInUowAsync("Tenant: Ground Forces", () => tenantDataSeeder.SeedTenantAsync(context, "GF", TenantScenario.FullApproved));
        //await ExecuteInUowAsync("Tenant: Air Forces", () => tenantDataSeeder.SeedTenantAsync(context, "AF", TenantScenario.MidWorkflow));
        //await ExecuteInUowAsync("Tenant: Naval Forces", () => tenantDataSeeder.SeedTenantAsync(context, "NF", TenantScenario.EarlyStage));

        logger.LogInformation("✅ GTMS Comprehensive Data Seeder — Complete!");
    }

    private async Task ExecuteInUowAsync(string stepName, Func<Task> action)
    {
        logger.LogInformation($"  ⏳ {stepName}...");
        using var uow = unitOfWorkManager.Begin(requiresNew: true);
        await action();
        await uow.CompleteAsync();
        logger.LogInformation($"  ✅ {stepName} — committed.");
    }
}

public enum TenantScenario
{
    FullApproved,
    MidWorkflow,
    EarlyStage,
}
