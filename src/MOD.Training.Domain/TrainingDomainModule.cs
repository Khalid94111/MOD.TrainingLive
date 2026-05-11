using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MOD.Training.Localization;
using MOD.Training.MultiTenancy;
using MOD.Training.Training.Payments.Storage;
using System;
using System.IO;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.FileSystem;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement.Identity;
using Volo.Abp.SettingManagement;
using Volo.Abp.BlobStoring.Database;
using Volo.Abp.Caching;
using Volo.Abp.OpenIddict;
using Volo.Abp.PermissionManagement.OpenIddict;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Emailing;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Commercial.SuiteTemplates;
using Volo.Abp.LanguageManagement;
using Volo.FileManagement;
using Volo.Abp.TextTemplateManagement;
using Volo.Saas;
using Volo.Abp.Gdpr;

namespace MOD.Training;

[DependsOn(
    typeof(TrainingDomainSharedModule),
    typeof(AbpCachingModule),
    typeof(AbpBackgroundJobsDomainModule),
    typeof(AbpFeatureManagementDomainModule),
    typeof(AbpPermissionManagementDomainIdentityModule),
    typeof(AbpPermissionManagementDomainOpenIddictModule),
    typeof(AbpSettingManagementDomainModule),
    typeof(AbpEmailingModule),
    typeof(AbpIdentityProDomainModule),
    typeof(AbpOpenIddictProDomainModule),
    typeof(SaasDomainModule),
    typeof(TextTemplateManagementDomainModule),
    typeof(LanguageManagementDomainModule),
    typeof(FileManagementDomainModule),
    typeof(VoloAbpCommercialSuiteTemplatesModule),
    typeof(AbpGdprDomainModule),
    typeof(BlobStoringDatabaseDomainModule),
    typeof(AbpBlobStoringFileSystemModule)
    )]
public class TrainingDomainModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = true;
        });

        ConfigureCourseInvoiceBlobStorage(context);

#if DEBUG
        context.Services.Replace(ServiceDescriptor.Singleton<IEmailSender, NullEmailSender>());
#endif
    }

    /// <summary>
    /// Phase 4B-β — file-system-backed BlobContainer for CoursePayment invoices.
    /// BasePath comes from <c>appsettings.json</c> (key <c>Gtms:Files:Path</c>); the same
    /// path is mirrored as ABP setting <c>Gtms.Files.Path</c> for visibility in the admin UI.
    /// Other ABP blob containers (file management, etc.) keep using BlobStoringDatabase —
    /// only this typed container swaps providers, so no broader change in storage behavior.
    /// </summary>
    private static void ConfigureCourseInvoiceBlobStorage(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var basePath = configuration["Gtms:Files:Path"];
        if (string.IsNullOrWhiteSpace(basePath))
            basePath = Path.Combine(Path.GetTempPath(), "gtms-files");

        // Ensure the directory exists; the FileSystem provider will fail at first write
        // otherwise. Per-tenant subfolders are auto-created by ABP at runtime.
        Directory.CreateDirectory(basePath);

        context.Services.Configure<AbpBlobStoringOptions>(options =>
        {
            options.Containers.Configure<CourseInvoiceContainer>(c =>
            {
                c.UseFileSystem(fs => fs.BasePath = basePath);
            });
        });
    }
}
