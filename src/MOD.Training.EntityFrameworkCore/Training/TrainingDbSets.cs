using Microsoft.EntityFrameworkCore;


namespace MOD.Training.Training;

/// <summary>
/// Add these DbSets to your existing YourAppDbContext class,
/// and call builder.ConfigureTraining() in OnModelCreating.
///
/// Then run:
///   dotnet ef migrations add AddTrainingModule
///   dotnet ef database update
/// </summary>
public static class TrainingDbSets
{
    /*
     * // ── Training Module: Catalog ──
     * public DbSet<CourseCatalog> CourseCatalogs => Set<CourseCatalog>();
     * public DbSet<CourseField> CourseFields => Set<CourseField>();
     * public DbSet<CatalogEnrollmentCondition> CatalogEnrollmentConditions => Set<CatalogEnrollmentCondition>();
     * public DbSet<CourseProposal> CourseProposals => Set<CourseProposal>();
     *
     * // ── Training Module: Tenant Courses ──
     * public DbSet<TenantCourse> TenantCourses => Set<TenantCourse>();
     * public DbSet<TenantCourseCondition> TenantCourseConditions => Set<TenantCourseCondition>();
     *
     * // ── Training Module: System ──
     * public DbSet<Country> Countries => Set<Country>();
     */
}
