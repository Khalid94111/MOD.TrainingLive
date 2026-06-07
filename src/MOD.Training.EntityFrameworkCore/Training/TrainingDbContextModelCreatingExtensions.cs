using Microsoft.EntityFrameworkCore;
using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Catalog;
using MOD.Training.Training.Centers;
using MOD.Training.Training.Consts;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Execution;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Nominations;
using MOD.Training.Training.Payments;
using MOD.Training.Training.Plans;
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
        builder.ConfigurePlans();
builder.ConfigureNominations();
builder.ConfigureFinancePhase3();
builder.ConfigureCasualCoursesPhase4A();
builder.ConfigurePreExecutionPhase4BAlpha();
builder.ConfigurePaymentsPhase4BBeta();
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

            // ParentId kept for schema compatibility (flat migration) but no FK constraint
            b.Property(x => x.ParentId).IsRequired(false);

            b.Property(x => x.DefaultAmountOMR).IsRequired().HasColumnType("decimal(18,3)");
            b.Property(x => x.ItemType).HasConversion<int?>();

            b.HasIndex(x => new { x.TenantId, x.NameAr });
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
            b.Property(x => x.Notes).HasMaxLength(500);

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
            b.Property(x => x.TotalAmount).HasPrecision(18, 3);
            b.Property(x => x.SpentAmount).HasPrecision(18, 3);
            b.Property(x => x.AlertThreshold).HasPrecision(5, 2);

            b.HasOne<FinancialItem>()
                .WithMany()
                .HasForeignKey(x => x.FinancialItemId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.Year, x.FinancialItemId })
                .IsUnique();
        });
    }
    private static void TrainingCenterConfiguration(this ModelBuilder builder)
    {
        builder.Entity<CenterRoleAssignment>(b =>
        {
            b.ToTable("TrnCenterRoleAssignments");
            b.ConfigureByConvention();

            b.Property(x => x.RoleType).HasConversion<string>().HasMaxLength(10);
            b.Property(x => x.AssignmentType).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.ServiceNumber).HasMaxLength(50);

            b.HasOne<TrainingCenter>()
                .WithMany()
                .HasForeignKey(x => x.CenterId)
                .OnDelete(DeleteBehavior.Cascade);

            // TCM: unique per center (only one TCM)
            // TCO: unique per center + employee/position (no duplicate assignments)
            // Enforced in AppService, not via EF index (conditional uniqueness)
        });
        builder.Entity<TrainingCenterPlan>(b =>
        {


            b.ToTable("TrnTrainingCenterPlans");
            b.ConfigureByConvention();

            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            b.HasOne<TrainingCenter>()
                .WithMany()
                .HasForeignKey(x => x.CenterId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<TrainingCenterPlanItem>(b =>
        {


            b.ToTable("TrnTrainingCenterPlanItems");
            b.ConfigureByConvention();

            b.Property(x => x.Objective).HasMaxLength(1000);
            b.Property(x => x.BeneficiaryType).HasConversion<string>().HasMaxLength(20);

            b.HasOne<TrainingCenterPlan>()
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<TrainingCenterPlanItemUnit>(b =>
        {
            b.ToTable("TrnTrainingCenterPlanItemUnits");
            b.ConfigureByConvention();

            b.HasOne<TrainingCenterPlanItem>()
                .WithMany()
                .HasForeignKey(x => x.PlanItemId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.PlanItemId, x.UnitId }).IsUnique();
        });
    }
 public static void ConfigurePlans(this ModelBuilder builder)
    {
        builder.Entity<TrainingPlan>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "TrainingPlans", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Year).IsRequired();
            b.Property(x => x.Status).IsRequired();

            b.HasIndex(x => new { x.TenantId, x.Year }).IsUnique();
        });

        builder.Entity<TrainingPlanItem>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "TrainingPlanItems", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.PlanId).IsRequired();
            b.Property(x => x.TenantCourseId).IsRequired();
            b.Property(x => x.CourseType).IsRequired();
            b.Property(x => x.PreferredQuarter).IsRequired();
            b.Property(x => x.Priority).IsRequired();
            b.Property(x => x.Justification).IsRequired().HasMaxLength(TrainingConsts.MaxJustificationLength);
            b.Property(x => x.DescriptionAr).HasMaxLength(TrainingConsts.MaxDescriptionLength);
            b.Property(x => x.DescriptionEn).HasMaxLength(TrainingConsts.MaxDescriptionLength);
            b.Property(x => x.ObjectivesAr).HasMaxLength(TrainingConsts.MaxObjectivesLength);
            b.Property(x => x.ObjectivesEn).HasMaxLength(TrainingConsts.MaxObjectivesLength);
            //b.Property(x => x.EstimatedCost).HasColumnType("decimal(18,3)");
            b.Property(x => x.FundingSource).HasMaxLength(TrainingConsts.MaxFundingSourceLength);
            b.Property(x => x.SubmittedById).IsRequired();

            b.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.PlanId);
            b.HasIndex(x => x.TenantCourseId);
        });

        builder.Entity<PlanItemCondition>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "PlanItemConditions", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.PlanItemId).IsRequired();
            b.Property(x => x.ConditionType).IsRequired();
            b.Property(x => x.ConditionValue).IsRequired().HasMaxLength(TrainingConsts.MaxConditionValueLength);

            b.HasOne(x => x.PlanItem).WithMany().HasForeignKey(x => x.PlanItemId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.PlanItemId);
        });

        builder.Entity<Course>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "Courses", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.TenantCourseId).IsRequired();
            b.Property(x => x.CourseType).IsRequired();
            b.Property(x => x.Status).IsRequired();

            b.HasIndex(x => x.TenantCourseId);
        });

        // Phase 4C-α (v4.10.0): polymorphic-source session execution. CHECK constraint
        // enforces exactly one of TrainingPlanItemId / TrainingCenterPlanItemId is set.
        // Foreign keys to the source plan items are declared as soft refs (no nav property,
        // no cascade) to keep the Phase 4C-β arm decoupled and avoid Phase-3 surface churn.
        builder.Entity<CourseSession>(b =>
        {
            b.ToTable(
                TrainingConsts.DbTablePrefix + "CourseSessions",
                TrainingConsts.DbSchema,
                t => t.HasCheckConstraint(
                    "CK_CourseSession_PolymorphicSource",
                    "([TrainingPlanItemId] IS NOT NULL AND [TrainingCenterPlanItemId] IS NULL) OR ([TrainingPlanItemId] IS NULL AND [TrainingCenterPlanItemId] IS NOT NULL)"));
            b.ConfigureByConvention();

            // Polymorphic source (XOR via CHECK).
            b.Property(x => x.TrainingPlanItemId).IsRequired(false);
            b.Property(x => x.TrainingCenterPlanItemId).IsRequired(false);

            b.Property(x => x.TenantCourseId).IsRequired();
            b.Property(x => x.CourseType).IsRequired();

            b.Property(x => x.PreferredQuarter).IsRequired();
            b.Property(x => x.PlanYear).IsRequired();

            // Nullable for external Planned sessions; set atomically by SelectPriceQuoteAsync.
            b.Property(x => x.ActualStartDate).IsRequired(false);
            b.Property(x => x.ActualEndDate).IsRequired(false);

            b.Property(x => x.SelectedPriceQuoteId).IsRequired(false);

            b.Property(x => x.Status).IsRequired().HasDefaultValue(SessionStatus.Planned);

            b.Property(x => x.CancellationReason).HasMaxLength(TrainingConsts.MaxNotesLength);
            b.Property(x => x.CancelledAt).IsRequired(false);
            b.Property(x => x.CancelledById).IsRequired(false);

            b.HasMany(x => x.Nominations)
                .WithOne()
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TenantId, x.TrainingPlanItemId });
            b.HasIndex(x => new { x.TenantId, x.TrainingCenterPlanItemId });
            b.HasIndex(x => new { x.TenantId, x.TenantCourseId });
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.PlanYear, x.PreferredQuarter });
        });

        builder.Entity<SessionNomination>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "SessionNominations", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.SessionId).IsRequired();
            b.Property(x => x.EmployeeId).IsRequired();
            b.Property(x => x.OriginalEmployeeId).IsRequired();
            b.Property(x => x.RankId).IsRequired();
            b.Property(x => x.SubstitutionReason).HasMaxLength(TrainingConsts.MaxNotesLength);

            // WasSubstituted is a [NotMapped] computed property.
            b.Ignore(x => x.WasSubstituted);

            b.HasIndex(x => new { x.TenantId, x.SessionId });
            b.HasIndex(x => new { x.TenantId, x.SessionId, x.EmployeeId }).IsUnique();
        });

        builder.Entity<SessionCondition>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "SessionConditions", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.SessionId).IsRequired();
            b.Property(x => x.ConditionType).IsRequired();
            b.Property(x => x.ConditionValue).IsRequired().HasMaxLength(TrainingConsts.MaxConditionValueLength);

            b.HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.SessionId);
        });
    }
     public static void ConfigureNominations(this ModelBuilder builder)
    {
        builder.Entity<Nomination>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "Nominations", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.SessionId).IsRequired(false);
            b.Property(x => x.PlanItemId).IsRequired();
            b.Property(x => x.EmployeeId).IsRequired();
            b.Property(x => x.NominatedById).IsRequired();
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.NominatedAt).IsRequired();
            b.Property(x => x.ResultValue).HasMaxLength(TrainingConsts.MaxCodeLength);

            b.HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
            b.HasOne(x => x.PlanItem).WithMany().HasForeignKey(x => x.PlanItemId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.PlanItemId);
            b.HasIndex(x => x.EmployeeId);
            b.HasIndex(x => new { x.PlanItemId, x.EmployeeId }).IsUnique();
        });

        builder.Entity<NominationApproval>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "NominationApprovals", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.NominationId).IsRequired();
            b.Property(x => x.ApprovalLevel).IsRequired();
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(TrainingConsts.MaxNotesLength);

            b.HasOne(x => x.Nomination).WithMany().HasForeignKey(x => x.NominationId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.NominationId);
            b.HasIndex(x => new { x.NominationId, x.ApprovalLevel }).IsUnique();
        });
    }
    public static void ConfigureFinancePhase3(this ModelBuilder builder)
    {
        builder.Entity<PlanItemFinancialItem>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "PlanItemFinancialItems", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.PlanItemId).IsRequired();
            b.Property(x => x.FinancialItemId).IsRequired();
            b.Property(x => x.EstimatedAmountOMR).IsRequired().HasColumnType("decimal(18,3)");
            b.Property(x => x.EstimatedAmountUSD).HasColumnType("decimal(18,2)");
            b.Property(x => x.ActualAmountOMR).HasColumnType("decimal(18,3)");
            b.Property(x => x.ActualAmountUSD).HasColumnType("decimal(18,2)");
            b.Property(x => x.Notes).HasMaxLength(TrainingConsts.MaxNotesLength);

            b.HasIndex(x => x.PlanItemId);
            b.HasIndex(x => new { x.PlanItemId, x.FinancialItemId }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        builder.Entity<PriceQuote>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "PriceQuotes", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            // Polymorphic parent — both nullable; DB-level CHECK constraint enforces "exactly one set".
            b.Property(x => x.SessionId).IsRequired(false);
            b.Property(x => x.CasualCourseId).IsRequired(false);

            b.Property(x => x.ProviderId).IsRequired();
            b.Property(x => x.PricingType).IsRequired();
            b.Property(x => x.QuotedPrice).IsRequired().HasColumnType("decimal(18,3)");
            b.Property(x => x.PricePerPerson).HasColumnType("decimal(18,3)");
            b.Property(x => x.TotalPrice).HasColumnType("decimal(18,3)");
            b.Property(x => x.ParticipantsCount).IsRequired();
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(TrainingConsts.MaxNotesLength);

            // Phase 4B-α additions
            b.Property(x => x.QuotedPriceOMR).HasColumnType("decimal(18,3)");
            b.Property(x => x.IsSelected).HasDefaultValue(false);

            b.HasOne(x => x.Provider).WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.SessionId);
            b.HasIndex(x => x.CasualCourseId);
            b.HasIndex(x => x.ProviderId);
            b.HasIndex(x => x.CountryId);
            b.HasIndex(x => x.CityId);
        });

        builder.Entity<TrainingProvider>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "TrainingProviders", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.ProviderNameAr).IsRequired().HasMaxLength(TrainingConsts.MaxProviderNameLength);
            b.Property(x => x.ProviderNameEn).IsRequired().HasMaxLength(TrainingConsts.MaxProviderNameLength);
            b.Property(x => x.ContactPerson).HasMaxLength(TrainingConsts.MaxContactPersonLength);
            b.Property(x => x.Email).HasMaxLength(TrainingConsts.MaxEmailLength);
            b.Property(x => x.Phone).HasMaxLength(TrainingConsts.MaxPhoneLength);
            b.Property(x => x.Address).HasMaxLength(TrainingConsts.MaxAddressLength);
            b.Property(x => x.Website).HasMaxLength(TrainingConsts.MaxWebsiteLength);
            b.Property(x => x.AverageRating).HasColumnType("decimal(3,2)");

            // Phase 4B-α additions
            b.Property(x => x.IsFromNebras).HasDefaultValue(false);
            b.Property(x => x.NebrasId).HasMaxLength(100);
            b.Property(x => x.Scope).IsRequired().HasDefaultValue(ProviderScope.Local);

            b.HasIndex(x => x.CountryId);
            b.HasIndex(x => x.IsFromNebras);
            b.HasIndex(x => x.Scope);
        });

        builder.Entity<FinancialItemRankAmount>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "FinancialItemRankAmounts", TrainingConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.FinancialItemId).IsRequired();
            b.Property(x => x.RankId).IsRequired();
            b.Property(x => x.AmountOMR).IsRequired().HasColumnType("decimal(18,3)");
            b.HasOne<FinancialItem>().WithMany().HasForeignKey(x => x.FinancialItemId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.TenantId, x.FinancialItemId, x.RankId }).IsUnique();
        });

        builder.Entity<PlanItemFinancialItemRank>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "PlanItemFinancialItemRanks", TrainingConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.PlanItemFinancialItemId).IsRequired();
            b.Property(x => x.RankId).IsRequired();
            b.Property(x => x.NomineeCount).IsRequired();
            b.Property(x => x.RatePerUnitOMR).IsRequired().HasColumnType("decimal(18,3)");
            b.Property(x => x.SubtotalOMR).IsRequired().HasColumnType("decimal(18,3)");
            b.HasOne<PlanItemFinancialItem>().WithMany().HasForeignKey(x => x.PlanItemFinancialItemId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.TenantId, x.PlanItemFinancialItemId, x.RankId }).IsUnique();
        });

        builder.Entity<PlanNote>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "PlanNotes", TrainingConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.EntityType).IsRequired();
            b.Property(x => x.EntityId).IsRequired();
            b.Property(x => x.Note).IsRequired().HasMaxLength(2000);
            b.Property(x => x.AuthorRole).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.EntityType, x.EntityId, x.CreationTime });
        });
    }

    public static void ConfigureCasualCoursesPhase4A(this ModelBuilder builder)
    {
        builder.Entity<CasualCourse>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "CasualCourses", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.TenantCourseId).IsRequired();
            b.Property(x => x.UnitId).IsRequired();
            b.Property(x => x.RequestedById).IsRequired();
            b.Property(x => x.CourseType).IsRequired();
            b.Property(x => x.Priority).IsRequired();
            b.Property(x => x.Justification).IsRequired().HasMaxLength(500);
            b.Property(x => x.DescriptionAr).HasMaxLength(1000);
            b.Property(x => x.ObjectivesAr).HasMaxLength(1000);
            b.Property(x => x.DurationDays).IsRequired();
            b.Property(x => x.EstimatedDateFrom).IsRequired();
            b.Property(x => x.EstimatedDateTo).IsRequired();
            b.Property(x => x.FundingSourceName).HasMaxLength(200);
            b.Property(x => x.FundingSourceVoteCode).HasMaxLength(100);
            b.Property(x => x.CourseCost).HasColumnType("decimal(18,3)");
            b.Property(x => x.EstimatedTotalCost).HasColumnType("decimal(18,3)");
            b.Property(x => x.RejectedReason).HasMaxLength(500);
            b.Property(x => x.Status).IsRequired();

            // Phase 4B-α — actual confirmed dates (written by SelectPriceQuoteAsync).
            b.Property(x => x.ActualStartDate).IsRequired(false);
            b.Property(x => x.ActualEndDate).IsRequired(false);

            b.HasMany(x => x.FinancialItems).WithOne()
                .HasForeignKey(x => x.CasualCourseId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Nominations).WithOne()
                .HasForeignKey(x => x.CasualCourseId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TenantId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.UnitId });
        });

        builder.Entity<CasualCourseFinancialItem>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "CasualCourseFinancialItems", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.CasualCourseId).IsRequired();
            b.Property(x => x.FinancialItemId).IsRequired();
            b.Property(x => x.EstimatedAmountOMR).IsRequired().HasColumnType("decimal(18,3)");
            b.Property(x => x.ActualAmountOMR).HasColumnType("decimal(18,3)");
            b.Property(x => x.Source).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(500);

            b.HasIndex(x => new { x.CasualCourseId, x.FinancialItemId }).IsUnique().HasFilter("[IsDeleted] = 0");

            // Ranks navigation declared on the rank-side block below (single relationship).
        });

        builder.Entity<CasualCourseNomination>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "CasualCourseNominations", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.CasualCourseId).IsRequired();
            b.Property(x => x.EmployeeId).IsRequired();

            b.HasIndex(x => new { x.CasualCourseId, x.EmployeeId }).IsUnique();
        });

        builder.Entity<CasualCourseFinancialItemRank>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "CasualCourseFinancialItemRanks", TrainingConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.CasualCourseFinancialItemId).IsRequired();
            b.Property(x => x.RankId).IsRequired();
            b.Property(x => x.NomineeCount).IsRequired();
            b.Property(x => x.RatePerUnitOMR).IsRequired().HasColumnType("decimal(18,3)");
            b.Property(x => x.SubtotalOMR).IsRequired().HasColumnType("decimal(18,3)");
            b.Property(x => x.RateSource).IsRequired().HasMaxLength(20);
            // Phase 4B-β — paired navigation: CasualCourseFinancialItem.Ranks ↔ this rank's FK.
            b.HasOne<CasualCourseFinancialItem>()
                .WithMany(p => p.Ranks)
                .HasForeignKey(x => x.CasualCourseFinancialItemId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.TenantId, x.CasualCourseFinancialItemId, x.RankId }).IsUnique();
        });
    }

    public static void ConfigurePreExecutionPhase4BAlpha(this ModelBuilder builder)
    {
        builder.Entity<TravelInstruction>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "TravelInstructions", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            // Polymorphic parent — both nullable; DB-level CHECK enforces exactly one set.
            b.Property(x => x.CasualCourseId).IsRequired(false);
            b.Property(x => x.SessionId).IsRequired(false);

            b.Property(x => x.DepartureDate).IsRequired();
            b.Property(x => x.ArrivalDate).IsRequired();
            b.Property(x => x.ReturnDate).IsRequired();
            b.Property(x => x.ArrivalBackDate).IsRequired();

            b.Property(x => x.VisaRequired).HasDefaultValue(false);
            b.Property(x => x.VisaNotes).HasMaxLength(500);
            b.Property(x => x.InsuranceArranged).HasDefaultValue(false);
            b.Property(x => x.InsuranceProvider).HasMaxLength(200);
            b.Property(x => x.TicketsBooked).HasDefaultValue(false);
            b.Property(x => x.TicketReference).HasMaxLength(100);

            b.Property(x => x.CalculatedTravelDays).IsRequired();
            b.Property(x => x.OverrideTravelDays).IsRequired(false);

            b.Property(x => x.Status).IsRequired().HasDefaultValue(TravelInstructionStatus.Draft);

            // Unique-per-arm: one travel instruction per casual course OR per session.
            b.HasIndex(x => x.CasualCourseId)
                .IsUnique()
                .HasFilter("[CasualCourseId] IS NOT NULL");
            b.HasIndex(x => x.SessionId)
                .IsUnique()
                .HasFilter("[SessionId] IS NOT NULL");
        });
    }

    /// <summary>
    /// Phase 4B-β — payments + auto-reallocation tables.
    /// Three new entities: TravelAllowancePayment + CoursePayment (both polymorphic with CHECK constraint)
    /// and BudgetReallocation (casual-course-only by design).
    /// </summary>
    public static void ConfigurePaymentsPhase4BBeta(this ModelBuilder builder)
    {
        builder.Entity<TravelAllowancePayment>(b =>
        {
            b.ToTable(
                TrainingConsts.DbTablePrefix + "TravelAllowancePayments",
                TrainingConsts.DbSchema,
                t => t.HasCheckConstraint(
                    "CK_TravelAllowancePayment_PolymorphicParent",
                    "([SessionId] IS NOT NULL AND [CasualCourseId] IS NULL) OR ([SessionId] IS NULL AND [CasualCourseId] IS NOT NULL)"));
            b.ConfigureByConvention();

            // Polymorphic parent.
            b.Property(x => x.SessionId).IsRequired(false);
            b.Property(x => x.CasualCourseId).IsRequired(false);

            b.Property(x => x.NominationId).IsRequired();
            b.Property(x => x.PersonnelType).IsRequired();

            b.Property(x => x.TicketAmountOMR).IsRequired().HasColumnType("decimal(18,3)");
            b.Property(x => x.TravelAllowanceOMR).IsRequired().HasColumnType("decimal(18,3)");
            b.Property(x => x.ClothingAllowanceOMR).IsRequired().HasColumnType("decimal(18,3)");
            b.Property(x => x.InsuranceOMR).IsRequired().HasColumnType("decimal(18,3)");
            b.Property(x => x.VisaFeesOMR).IsRequired().HasColumnType("decimal(18,3)");
            b.Property(x => x.TotalOMR).IsRequired().HasColumnType("decimal(18,3)");

            b.Property(x => x.Status).IsRequired().HasDefaultValue(PaymentStatus.Draft);
            b.Property(x => x.ConfirmedAt).IsRequired(false);
            b.Property(x => x.ConfirmedById).IsRequired(false);

            b.Property(x => x.ExternalRequestId).HasMaxLength(100);
            b.Property(x => x.ExternalStatus).HasMaxLength(50);
            b.Property(x => x.ExternalResponseAt).IsRequired(false);

            b.Property(x => x.Notes).HasMaxLength(TrainingConsts.MaxNotesLength);

            // One TravelAllowancePayment per nomination, tenant-scoped.
            b.HasIndex(x => new { x.TenantId, x.NominationId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.SessionId });
            b.HasIndex(x => new { x.TenantId, x.CasualCourseId });
            b.HasIndex(x => new { x.TenantId, x.Status });
        });

        builder.Entity<CoursePayment>(b =>
        {
            b.ToTable(
                TrainingConsts.DbTablePrefix + "CoursePayments",
                TrainingConsts.DbSchema,
                t => t.HasCheckConstraint(
                    "CK_CoursePayment_PolymorphicParent",
                    "([SessionId] IS NOT NULL AND [CasualCourseId] IS NULL) OR ([SessionId] IS NULL AND [CasualCourseId] IS NOT NULL)"));
            b.ConfigureByConvention();

            // Polymorphic parent.
            b.Property(x => x.SessionId).IsRequired(false);
            b.Property(x => x.CasualCourseId).IsRequired(false);

            b.Property(x => x.TrainingProviderId).IsRequired();

            b.Property(x => x.InvoiceAmountOMR).IsRequired().HasColumnType("decimal(18,3)");
            b.Property(x => x.InvoiceDate).IsRequired();

            // BlobStoring file refs — populated only by UploadInvoiceAsync.
            b.Property(x => x.InvoiceBlobName).HasMaxLength(200);
            b.Property(x => x.InvoiceOriginalFileName).HasMaxLength(260);

            b.Property(x => x.Status).IsRequired().HasDefaultValue(PaymentStatus.Draft);
            b.Property(x => x.ConfirmedAt).IsRequired(false);
            b.Property(x => x.ConfirmedById).IsRequired(false);

            b.Property(x => x.Notes).HasMaxLength(TrainingConsts.MaxNotesLength);

            b.HasIndex(x => new { x.TenantId, x.SessionId });
            b.HasIndex(x => new { x.TenantId, x.CasualCourseId });
            b.HasIndex(x => new { x.TenantId, x.TrainingProviderId });
            b.HasIndex(x => new { x.TenantId, x.Status });
        });

        builder.Entity<BudgetReallocation>(b =>
        {
            b.ToTable(TrainingConsts.DbTablePrefix + "BudgetReallocations", TrainingConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.CasualCourseId).IsRequired();
            b.Property(x => x.CoursePaymentId).IsRequired();
            b.Property(x => x.FundingSourceVoteCode).IsRequired().HasMaxLength(TrainingConsts.MaxFundingSourceLength);
            b.Property(x => x.ToFinancialItemId).IsRequired();
            b.Property(x => x.AmountOMR).IsRequired().HasColumnType("decimal(18,3)");

            b.Property(x => x.Status).IsRequired().HasDefaultValue(ReallocationStatus.Pending);
            b.Property(x => x.ApprovedAt).IsRequired(false);
            b.Property(x => x.ApprovedById).IsRequired(false);
            b.Property(x => x.ApprovalNote).HasMaxLength(TrainingConsts.MaxNotesLength);

            b.HasIndex(x => new { x.TenantId, x.CasualCourseId });
            b.HasIndex(x => new { x.TenantId, x.CoursePaymentId });
            b.HasIndex(x => new { x.TenantId, x.Status });
        });
    }
}
