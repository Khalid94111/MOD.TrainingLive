using System;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Uow;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.SqlServer;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.LanguageManagement.EntityFrameworkCore;
using Volo.FileManagement.EntityFrameworkCore;
using Volo.Abp.TextTemplateManagement.EntityFrameworkCore;
using Volo.Saas.EntityFrameworkCore;
using Volo.Abp.Gdpr;
using Volo.Abp.Studio;
using Travel.EntityFrameworkCore;

namespace MOD.Training.EntityFrameworkCore;

[DependsOn(typeof(TrainingDomainModule), typeof(AbpPermissionManagementEntityFrameworkCoreModule), typeof(AbpSettingManagementEntityFrameworkCoreModule), typeof(AbpEntityFrameworkCoreSqlServerModule), typeof(AbpBackgroundJobsEntityFrameworkCoreModule), typeof(AbpFeatureManagementEntityFrameworkCoreModule), typeof(AbpIdentityProEntityFrameworkCoreModule), typeof(AbpOpenIddictProEntityFrameworkCoreModule), typeof(LanguageManagementEntityFrameworkCoreModule), typeof(FileManagementEntityFrameworkCoreModule), typeof(SaasEntityFrameworkCoreModule), typeof(TextTemplateManagementEntityFrameworkCoreModule), typeof(AbpGdprEntityFrameworkCoreModule), typeof(BlobStoringDatabaseEntityFrameworkCoreModule), typeof(TravelEntityFrameworkCoreModule))]
public class TrainingEntityFrameworkCoreModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        TrainingEfCoreEntityExtensionMappings.Configure();
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<TrainingDbContext>(options => {
            /* Remove "includeAllEntities: true" to create
                 * default repositories only for aggregate roots */
            options.AddDefaultRepositories(includeAllEntities: true);
         });
        if (AbpStudioAnalyzeHelper.IsInAnalyzeMode)
        {
            return;
        }

        Configure<AbpDbContextOptions>(options => {
            /* The main point to change your DBMS.
             * See also TrainingDbContextFactory for EF Core tooling. */
            options.UseSqlServer();
        });
    }
}