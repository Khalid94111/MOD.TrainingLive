using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MOD.Training.Training;
using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Volo.Saas.Editions;
using Volo.Saas.Tenants;

namespace MOD.Training.Saas;
 
public class SaasDataSeedContributor //: IDataSeedContributor, ITransientDependency
{
    private readonly IEditionDataSeeder _editionDataSeeder;
    private readonly ICurrentTenant _currentTenant;
    private readonly IRepository<Edition, Guid> _editionRepository;
    private readonly IRepository<Tenant, Guid> _tenantRepository;
    private readonly ITenantManager _tenantManager;
     private readonly IGuidGenerator _guidGenerator;
     private readonly IServiceProvider _serviceProvider;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IdentityRoleManager roleManager;
    private enum SeedScenario { FullApproved, MidWorkflow, EarlyStage }

    public SaasDataSeedContributor(IEditionDataSeeder editionDataSeeder, ICurrentTenant currentTenant, IRepository<Edition, Guid> editionRepository, IRepository<Tenant, Guid> tenantRepository, ITenantManager tenantManager, IGuidGenerator guidGenerator, IServiceProvider serviceProvider, IUnitOfWorkManager unitOfWorkManager, IdentityRoleManager roleManager)
    {
        _editionDataSeeder = editionDataSeeder;
        _currentTenant = currentTenant;
        _editionRepository = editionRepository;
        _tenantRepository = tenantRepository;
        _tenantManager = tenantManager;
        _guidGenerator = guidGenerator;
        _serviceProvider = serviceProvider;
        _unitOfWorkManager = unitOfWorkManager;
        this.roleManager = roleManager;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        using (_currentTenant.Change(context?.TenantId))
        {
            await _editionDataSeeder.CreateStandardEditionsAsync();
            await _unitOfWorkManager.Current.SaveChangesAsync();
            ////logger.LogInformation("🚀 GTMS Comprehensive Data Seeder — Starting...");

            var stanadrdEdition = await _editionRepository.GetAsync(c => c.DisplayName == "Standard");
            if (stanadrdEdition != null)
            {
                var cossafTenant = await _tenantRepository.FindAsync(c => c.Name == "COSSAF", false);

                if (cossafTenant == null)
                {
                    cossafTenant = await _tenantManager.CreateAsync("COSSAF", stanadrdEdition.Id);
                    cossafTenant.SetProperty("EnglishDescription", "Chief of Staff of the Armed Forces");
                    cossafTenant.SetProperty("ArabicDescription", "رئاسة قوات السلطان المسلحة");

                     cossafTenant = await _tenantRepository.InsertAsync(cossafTenant, true);

                    using ( _currentTenant.Change(cossafTenant.Id))
                    {
                       //var sss= _currentTenant.Name;
 
                       // await roleManager.CreateAsync(new IdentityRole(_guidGenerator.Create(), "GF", cossafTenant.Id));

                        //await SeedTenantAsync(cossafTenant.Id, "GF", SeedScenario.FullApproved);
                        //await SeedTenantAsync(cossafTenant.Id, "AF", SeedScenario.MidWorkflow);
                        //await SeedTenantAsync(cossafTenant.Id, "NF", SeedScenario.EarlyStage);
                    }
                }


                var raoTenant = await _tenantRepository.FindAsync(c=>c.Name== "RAO", false);

                if (raoTenant == null)
                {
                    raoTenant = await _tenantManager.CreateAsync("RAO", stanadrdEdition.Id);
                    raoTenant.SetProperty("EnglishDescription", "Royal Army of Oman");
                    raoTenant.SetProperty("ArabicDescription", "الجيش السلطاني العماني");

                    await _tenantRepository.InsertAsync(raoTenant, true);

                    //using (_currentTenant.Change(raoTenant.Id))
                    //{
                    //    await DataSeeder.SeedAsync(
                    //        new DataSeedContext(raoTenant.Id)
                    //            .WithProperty("AdminEmail", "rao.jund@mod.saf")
                    //            .WithProperty("AdminPassword", "1q2w3E*")
                    //    );
                    //}


                }


                //var rafoTenant = await _tenantRepository.FindAsync(c => c.Name == "RAFO", false);

                //if (rafoTenant == null)
                //{
                //    rafoTenant = await _tenantManager.CreateAsync("RAFO", stanadrdEdition.Id);
                //    rafoTenant.SetProperty("EnglishDescription", "Royal Air Forec of Oman");
                //    rafoTenant.SetProperty("ArabicDescription", "سلاح الجو السلطاني العماني");

                //    await _tenantRepository.InsertAsync(rafoTenant, true);

                //    //using (_currentTenant.Change(rafoTenant.Id))
                //    //{
                //    //    await DataSeeder.SeedAsync(
                //    //        new DataSeedContext(rafoTenant.Id)
                //    //            .WithProperty("AdminEmail", "rafo.jund@mod.saf")
                //    //            .WithProperty("AdminPassword", "1q2w3E*")
                //    //    );
                //    //}
                //}

                var rnoTenant = await _tenantRepository.FindAsync(c => c.Name == "RNO", false);

                if (rnoTenant == null)
                {
                    rnoTenant = await _tenantManager.CreateAsync("RNO", stanadrdEdition.Id);
                    rnoTenant.SetProperty("EnglishDescription", "Royal Navy Of Oman");
                    rnoTenant.SetProperty("ArabicDescription", "البحرية السلطانية العمانية");

                    await _tenantRepository.InsertAsync(rnoTenant, true);

                    //using (_currentTenant.Change(rnoTenant.Id))
                    //{
                    //    await DataSeeder.SeedAsync(
                    //        new DataSeedContext(rnoTenant.Id)
                    //            .WithProperty("AdminEmail", "rno.jund@mod.saf")
                    //            .WithProperty("AdminPassword", "1q2w3E*")
                    //    );
                    //}

                }



            }
        }
    }
    [UnitOfWork]

    private async Task SeedTenantAsync(Guid tenantId, string prefix, SeedScenario scenario)
    {
        //logger.LogInformation($"  🏛️ Seeding Tenant: {prefix} (scenario: {scenario})...");

        // 1. Roles
        await SeedRolesAsync();
    }
    [UnitOfWork]

    private async Task SeedRolesAsync()
    {
        var roleNames = new[]
        {
            "TenantHead", "TrainingDirector", "TrainingDirectorateStaff",
            "SchedulingOfficer", "FinanceOfficer", "ExternalCoursesOfficer",
            "UnitGeneralManager", "UnitTrainingManager", "DirectManager",
            "Employee", "TrainingCenterOfficer", "TrainingCenterManager",
            "InternalTrainingOfficer", "HROfficer", "HRDirector"
        };

        foreach (var name in roleNames)
        {
            if (await roleManager.FindByNameAsync(name) == null)
            {
                await roleManager.CreateAsync(new IdentityRole(_guidGenerator.Create(), name));
            }
        }
    }

}
