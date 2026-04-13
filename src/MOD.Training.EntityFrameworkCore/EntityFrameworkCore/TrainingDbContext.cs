using Microsoft.EntityFrameworkCore;
using MOD.Training.Books;
using MOD.Training.Oranges;
using MOD.Training.Training;
using MOD.Training.Training.Catalog;
using MOD.Training.Training.Centers;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Nominations;
using MOD.Training.Training.Plans;
using MOD.Training.Training.System;
using MOD.Training.Training.TenantCourses;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Gdpr;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.LanguageManagement.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TextTemplateManagement.EntityFrameworkCore;
using Volo.FileManagement.EntityFrameworkCore;
using Volo.Saas.Editions;
using Volo.Saas.EntityFrameworkCore;
using Volo.Saas.Tenants;

namespace MOD.Training.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityProDbContext))]
[ReplaceDbContext(typeof(ISaasDbContext))]
[ConnectionStringName("Default")]
public class TrainingDbContext : AbpDbContext<TrainingDbContext>, ISaasDbContext, IIdentityProDbContext
{
      // ── Training Module: Catalog ──
      public DbSet<CourseCatalog> CourseCatalogs => Set<CourseCatalog>();
      public DbSet<CourseField> CourseFields => Set<CourseField>();
      public DbSet<CatalogEnrollmentCondition> CatalogEnrollmentConditions => Set<CatalogEnrollmentCondition>();
      public DbSet<CourseProposal> CourseProposals => Set<CourseProposal>();
     
      // ── Training Module: Tenant Courses ──
      public DbSet<TenantCourse> TenantCourses => Set<TenantCourse>();
      public DbSet<TenantCourseCondition> TenantCourseConditions => Set<TenantCourseCondition>();
     
      // ── Training Module: System ──
     public DbSet<Country> Countries => Set<Country>();

    // Phase 2A: Finance
    public DbSet<FinancialItem> FinancialItems { get; set; } = default!;
    public DbSet<CourseTypeFinancialItemDefault> CourseTypeFinancialItemDefaults { get; set; } = default!;
    public DbSet<ExchangeRate> ExchangeRates { get; set; } = default!;
    public DbSet<TrainingBudget> TrainingBudgets { get; set; } = default!;

    
    // --- Centers (Phase 2B) ---
    public DbSet<TrainingCenter> TrainingCenters { get; set; }
    public DbSet<CenterPlanWindow> CenterPlanWindows { get; set; }
    public DbSet<CenterRoleAssignment> CenterRoleAssignments { get; set; }
    public DbSet<TrainingCenterPlan> TrainingCenterPlans { get; set; }
    public DbSet<TrainingCenterPlanItem> TrainingCenterPlanItems { get; set; }
    public DbSet<TrainingCenterPlanItemUnit> TrainingCenterPlanItemUnits { get; set; }

// Plans
public DbSet<TrainingPlan> TrainingPlans { get; set; }
public DbSet<TrainingPlanItem> TrainingPlanItems { get; set; }
public DbSet<PlanItemCondition> PlanItemConditions { get; set; }
public DbSet<Course> Courses { get; set; }
public DbSet<CourseSession> CourseSessions { get; set; }
public DbSet<SessionCondition> SessionConditions { get; set; }

// Finance (Phase 3 additions)
public DbSet<PlanItemFinancialItem> PlanItemFinancialItems { get; set; }
public DbSet<PriceQuote> PriceQuotes { get; set; }
public DbSet<TrainingProvider> TrainingProviders { get; set; }

// Nominations
public DbSet<Nomination> Nominations { get; set; }
public DbSet<NominationApproval> NominationApprovals { get; set; }




    public DbSet<Orange> Oranges { get; set; } = null!;

    /* Add DbSet properties for your Aggregate Roots / Entities here. */
    public DbSet<Book> Books { get; set; }

    #region Entities from the modules
    /* Notice: We only implemented IIdentityProDbContext and ISaasDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityProDbContext and ISaasDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */
    // Identity
    public DbSet<IdentityUser> Users { get; set; }

    public DbSet<IdentityRole> Roles { get; set; }

    public DbSet<IdentityClaimType> ClaimTypes { get; set; }

    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }

    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }

    public DbSet<IdentityLinkUser> LinkUsers { get; set; }

    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }

    public DbSet<IdentitySession> Sessions { get; set; }

    // SaaS
    public DbSet<Tenant> Tenants { get; set; }

    public DbSet<Edition> Editions { get; set; }

    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }
    #endregion
    public TrainingDbContext(DbContextOptions<TrainingDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        /* Include modules to your migration db context */
        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentityPro();
        builder.ConfigureOpenIddictPro();
        builder.ConfigureLanguageManagement();
        builder.ConfigureFileManagement();
        builder.ConfigureSaas();
        builder.ConfigureTextTemplateManagement();
        builder.ConfigureGdpr();
        builder.ConfigureBlobStoring();
        builder.ConfigureTraining();


        builder.Entity<Book>(b => {
            b.ToTable(TrainingConsts.DbTablePrefix + "Books", TrainingConsts.DbSchema);
            b.ConfigureByConvention();
            //auto configure for the base class props
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        });
        /* Configure your own tables/entities inside here */
        //builder.Entity<YourEntity>(b =>
        //{
        //    b.ToTable(TrainingConsts.DbTablePrefix + "YourEntities", TrainingConsts.DbSchema);
        //    b.ConfigureByConvention(); //auto configure for the base class props
        //    //...
        //});
        if (builder.IsHostDatabase())
        {
            builder.Entity<Orange>(b => {
                b.ToTable(TrainingConsts.DbTablePrefix + "Oranges", TrainingConsts.DbSchema);
                b.ConfigureByConvention();
                b.Property(x => x.ArabicName).HasColumnName(nameof(Orange.ArabicName)).IsRequired();
            });
        }
    }
}