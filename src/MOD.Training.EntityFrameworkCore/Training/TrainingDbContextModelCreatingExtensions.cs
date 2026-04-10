using Microsoft.EntityFrameworkCore;
using MOD.Training.Training.Catalog;
using MOD.Training.Training.Consts;
using MOD.Training.Training.Finance;
using MOD.Training.Training.System;
using MOD.Training.Training.TenantCourses;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace MOD.Training.Training;

public static class TrainingDbContextModelCreatingExtensions
{
    /// <summary>
    /// Call from your main DbContext's OnModelCreating:
    ///   builder.ConfigureTraining();
    /// </summary>
    public static void ConfigureTraining(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.ConfigureCatalog();
        builder.ConfigureTenantCourses();
        builder.ConfigureTrainingSystem();
        builder.ConfigureFinance();
    }

    private static void ConfigureCatalog(this ModelBuilder builder)
    {
        builder.Entity<CourseCatalog>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "CourseCatalog", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.CourseNameAr).IsRequired().HasMaxLength(TrainingConsts.MaxCourseNameLength);
            b.Property(x => x.CourseNameEn).IsRequired().HasMaxLength(TrainingConsts.MaxCourseNameLength);
            b.Property(x => x.DescriptionAr).HasMaxLength(TrainingConsts.MaxDescriptionLength);
            b.Property(x => x.DescriptionEn).HasMaxLength(TrainingConsts.MaxDescriptionLength);
            b.Property(x => x.Category).IsRequired().HasMaxLength(TrainingConsts.MaxCategoryLength);
            b.Property(x => x.Nature).IsRequired().HasMaxLength(TrainingConsts.MaxNatureLength);

            b.HasIndex(x => x.CourseNameAr).IsUnique();
            b.HasIndex(x => x.CourseNameEn).IsUnique();

            b.HasOne(x => x.Field)
                .WithMany()
                .HasForeignKey(x => x.FieldId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CourseField>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "CourseFields", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.FieldNameAr).IsRequired().HasMaxLength(TrainingConsts.MaxFieldNameLength);
            b.Property(x => x.FieldNameEn).IsRequired().HasMaxLength(TrainingConsts.MaxFieldNameLength);
        });

        builder.Entity<CatalogEnrollmentCondition>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "CatalogEnrollmentConditions", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.ConditionValue).IsRequired().HasMaxLength(TrainingConsts.MaxConditionValueLength);

            b.HasOne(x => x.CatalogCourse)
                .WithMany()
                .HasForeignKey(x => x.CatalogCourseId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.CatalogCourseId);
        });

        builder.Entity<CourseProposal>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "CourseProposals", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.CourseNameAr).IsRequired().HasMaxLength(TrainingConsts.MaxCourseNameLength);
            b.Property(x => x.CourseNameEn).IsRequired().HasMaxLength(TrainingConsts.MaxCourseNameLength);
            b.Property(x => x.Category).IsRequired().HasMaxLength(TrainingConsts.MaxCategoryLength);
            b.Property(x => x.Nature).IsRequired().HasMaxLength(TrainingConsts.MaxNatureLength);
            b.Property(x => x.RejectionReason).HasMaxLength(TrainingConsts.MaxRejectionReasonLength);

            b.HasOne(x => x.Field)
                .WithMany()
                .HasForeignKey(x => x.FieldId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.ProposedById);
        });
    }

    private static void ConfigureTenantCourses(this ModelBuilder builder)
    {
        builder.Entity<TenantCourse>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "TenantCourses", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.HasIndex(x => new { x.TenantId, x.CatalogCourseId }).IsUnique();

            b.HasOne(x => x.CatalogCourse)
                .WithMany()
                .HasForeignKey(x => x.CatalogCourseId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.TenantId);
        });

        builder.Entity<TenantCourseCondition>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "TenantCourseConditions", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.ConditionValue).IsRequired().HasMaxLength(TrainingConsts.MaxConditionValueLength);

            b.HasOne(x => x.TenantCourse)
                .WithMany()
                .HasForeignKey(x => x.TenantCourseId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.TenantCourseId);
        });
    }

    private static void ConfigureTrainingSystem(this ModelBuilder builder)
    {
        builder.Entity<Country>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "Countries", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.NameAr).IsRequired().HasMaxLength(TrainingConsts.MaxCountryNameLength);
            b.Property(x => x.NameEn).IsRequired().HasMaxLength(TrainingConsts.MaxCountryNameLength);
            b.Property(x => x.Code).IsRequired().HasMaxLength(TrainingConsts.MaxCountryCodeLength);

            b.HasIndex(x => x.Code).IsUnique();
        });
    }
    public static void ConfigureFinance(this ModelBuilder builder)
    {
        // ============================================================
        // TrnFinancialItems
        // ============================================================
        builder.Entity<FinancialItem>(b =>
        {
            b.ToTable("TrnFinancialItems");
            b.ConfigureByConvention();

            b.Property(x => x.NameAr).IsRequired().HasMaxLength(200);
            b.Property(x => x.NameEn).IsRequired().HasMaxLength(200);
            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.VoteCode).HasMaxLength(50);

            b.HasOne(x => x.Parent)
                .WithMany()
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.ParentId });
        });

        // ============================================================
        // TrnCourseTypeFinancialItemDefaults
        // ============================================================
        builder.Entity<CourseTypeFinancialItemDefault>(b =>
        {
            b.ToTable("TrnCourseTypeFinancialItemDefaults");
            b.ConfigureByConvention();

            b.Property(x => x.CourseType).IsRequired();
            b.Property(x => x.SortOrder).IsRequired();

            b.HasOne(x => x.FinancialItem)
                .WithMany()
                .HasForeignKey(x => x.FinancialItemId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.CourseType, x.FinancialItemId })
                .IsUnique();
        });

        // ============================================================
        // TrnExchangeRates
        // ============================================================
        builder.Entity<ExchangeRate>(b =>
        {
            b.ToTable("TrnExchangeRates");
            b.ConfigureByConvention();

            b.Property(x => x.FromCurrency).IsRequired().HasMaxLength(10);
            b.Property(x => x.ToCurrency).IsRequired().HasMaxLength(10);
            b.Property(x => x.Rate).HasPrecision(18, 6);

            b.HasIndex(x => x.IsActive);
        });

        // ============================================================
        // TrnTrainingBudgets
        // ============================================================
        builder.Entity<TrainingBudget>(b =>
        {
            b.ToTable("TrnTrainingBudgets");
            b.ConfigureByConvention();

            b.Property(x => x.Year).IsRequired();
            b.Property(x => x.BudgetType).IsRequired();
            b.Property(x => x.TotalAmount).HasPrecision(18, 3);
            b.Property(x => x.SpentAmount).HasPrecision(18, 3);
            b.Property(x => x.AlertThreshold).HasPrecision(5, 2);

            b.HasIndex(x => new { x.TenantId, x.Year, x.BudgetType })
                .IsUnique();
        });
    }

}
