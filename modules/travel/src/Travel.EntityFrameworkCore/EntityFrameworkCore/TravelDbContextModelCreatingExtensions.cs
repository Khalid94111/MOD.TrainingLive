using Microsoft.EntityFrameworkCore;
using Travel.Allowances;
using Travel.TravelRequests;
using Travel.TravelTypes;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Travel.EntityFrameworkCore;

public static class TravelDbContextModelCreatingExtensions
{
    public static void ConfigureTravelManagement(this ModelBuilder builder)
    {
        builder.ConfigureTravel();
    }

    public static void ConfigureTravel(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<TravelRequest>(b =>
        {
            b.ToTable(TravelDbProperties.DbTablePrefix + "TravelRequests", TravelDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Title).IsRequired().HasMaxLength(TravelRequestConsts.MaxTitleLength);
            b.Property(x => x.Description).HasMaxLength(TravelRequestConsts.MaxDescriptionLength);
            b.Property(x => x.DestinationCountry).IsRequired().HasMaxLength(TravelRequestConsts.MaxCountryLength);
            b.Property(x => x.DestinationCity).IsRequired().HasMaxLength(TravelRequestConsts.MaxCityLength);
            b.Property(x => x.Department).HasMaxLength(TravelRequestConsts.MaxDepartmentLength);
            b.Property(x => x.Currency).HasMaxLength(TravelRequestConsts.MaxCurrencyLength);
            b.Property(x => x.AllowanceTiers).HasMaxLength(TravelRequestConsts.MaxAllowanceTiersLength);
            b.Property(x => x.Category).IsRequired();
            b.Property(x => x.DailyAllowanceRate).HasColumnType("decimal(18,2)");
            b.Property(x => x.RequesterName).IsRequired().HasMaxLength(256);
            b.Property(x => x.VisaCostPerEmployee).HasColumnType("decimal(18,2)");
            b.Property(x => x.TravelInsuranceCostPerEmployee).HasColumnType("decimal(18,2)");
            b.Property(x => x.TicketAirline).IsRequired().HasMaxLength(128);
            b.Property(x => x.TicketFlightNumber).IsRequired().HasMaxLength(64);
            b.Property(x => x.TicketCostPerEmployee).HasColumnType("decimal(18,2)");
            b.Property(x => x.TravelOfficeNotes).IsRequired().HasMaxLength(1000);
            b.Property(x => x.SourceSystem).HasMaxLength(TravelRequestConsts.MaxSourceSystemLength);
            b.Property(x => x.SourceTrainingCourseName).HasMaxLength(TravelRequestConsts.MaxSourceTrainingCourseNameLength);
            b.Property(x => x.FundingSourceVoteCode).HasMaxLength(TravelRequestConsts.MaxFundingSourceVoteCodeLength);
            b.Property(x => x.TicketFundingSourceVoteCode).HasMaxLength(TravelRequestConsts.MaxFundingSourceVoteCodeLength);
            b.Property(x => x.VisaFundingSourceVoteCode).HasMaxLength(TravelRequestConsts.MaxFundingSourceVoteCodeLength);
            b.Property(x => x.HealthInsuranceFundingSourceVoteCode).HasMaxLength(TravelRequestConsts.MaxFundingSourceVoteCodeLength);
            b.Property(x => x.DailyAllowanceFundingSourceVoteCode).HasMaxLength(TravelRequestConsts.MaxFundingSourceVoteCodeLength);
            b.Property(x => x.ClothingAllowanceFundingSourceVoteCode).HasMaxLength(TravelRequestConsts.MaxFundingSourceVoteCodeLength);
            b.Property(x => x.IntegrationWarnings).HasMaxLength(TravelRequestConsts.MaxIntegrationWarningsLength);

            b.HasIndex(x => x.TravelTypeDefinitionId);
            b.HasIndex(x => x.Status);
            b.HasOne<TravelTypeDefinition>().WithMany().HasForeignKey(x => x.TravelTypeDefinitionId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.RequesterId);
            b.HasIndex(x => x.Department);
            b.HasIndex(x => x.Category);
            b.HasIndex(x => x.CreationTime);
            b.HasIndex(x => x.SelectedDepartureFlightOfferId);
            b.HasIndex(x => x.SelectedReturnFlightOfferId);
            b.HasIndex(x => x.SourceTenantId);
            b.HasIndex(x => x.SourceTrainingCourseId);

            b.HasMany(x => x.Employees).WithOne().HasForeignKey(e => e.TravelRequestId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Documents).WithOne().HasForeignKey(d => d.TravelRequestId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.FlightOffers).WithOne().HasForeignKey(e => e.TravelRequestId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.EmployeeDocuments).WithOne().HasForeignKey(e => e.TravelRequestId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.AllowanceDetails).WithOne().HasForeignKey(e => e.TravelRequestId).OnDelete(DeleteBehavior.Cascade);

            b.OwnsOne(e => e.AllowanceSnapshot, snap =>
            {
                snap.Property(s => s.OverseasTotal).HasColumnType("decimal(18,2)");
                snap.Property(s => s.ClothingTotal).HasColumnType("decimal(18,2)");
                snap.Property(s => s.DeductionAmount).HasColumnType("decimal(18,2)");
                snap.Property(s => s.CalculatedAt);
            });
        });

        builder.Entity<TravelFundingSourceVoteRule>(b =>
        {
            b.ToTable(TravelDbProperties.DbTablePrefix + "FundingSourceVoteRules", TravelDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.PaymentType).IsRequired().HasMaxLength(50);
            b.Property(x => x.FundingSourceVoteCode).IsRequired().HasMaxLength(TravelRequestConsts.MaxFundingSourceVoteCodeLength);
            b.Property(x => x.IsActive).IsRequired();

            b.HasIndex(x => x.PaymentType).IsUnique();
            b.HasIndex(x => x.IsActive);
        });

        builder.Entity<TravelRequestEmployee>(b =>
        {
            b.ToTable(TravelDbProperties.DbTablePrefix + "TravelRequestEmployees", TravelDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.EmployeeName).IsRequired().HasMaxLength(200);
            b.Property(x => x.EmployeeNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.DailyAllowanceRate).HasColumnType("decimal(18,2)");
            b.Property(x => x.Category).IsRequired();
            b.Property(x => x.TicketClass).IsRequired();
            b.HasIndex(x => x.TravelRequestId);
            b.HasIndex(x => x.EmployeeId);
            b.HasIndex(x => x.EmployeeNumber);
            b.HasIndex(x => x.Category);
            b.HasIndex(x => x.TicketClass);
        });

        builder.Entity<TravelDocument>(b =>
        {
            b.ToTable(TravelDbProperties.DbTablePrefix + "TravelDocuments", TravelDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(TravelRequestConsts.MaxDocumentNameLength);
            b.Property(x => x.FileExtension).IsRequired().HasMaxLength(TravelRequestConsts.MaxFileExtensionLength);
            b.Property(x => x.BlobName).IsRequired().HasMaxLength(500);
            b.HasIndex(x => x.TravelRequestId);
        });

        builder.Entity<TravelFlightOffer>(b =>
        {
            b.ToTable(TravelDbProperties.DbTablePrefix + "TravelFlightOffers", TravelDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.TicketClass).IsRequired();
            b.Property(x => x.Airline).IsRequired().HasMaxLength(128);
            b.Property(x => x.FlightNumber).IsRequired().HasMaxLength(64);
            b.Property(x => x.Price).HasColumnType("decimal(18,2)");
            b.Property(x => x.Currency).IsRequired().HasMaxLength(10);
            b.Property(x => x.Duration).IsRequired().HasMaxLength(64);
            b.HasIndex(x => x.TravelRequestId);
            b.HasIndex(x => x.Direction);
            b.HasIndex(x => x.TicketClass);
            b.HasIndex(x => x.IsSelected);
        });

        builder.Entity<TravelEmployeeDocument>(b =>
        {
            b.ToTable(TravelDbProperties.DbTablePrefix + "TravelEmployeeDocuments", TravelDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.EmployeeName).IsRequired().HasMaxLength(200);
            b.Property(x => x.EmployeeNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.TicketNumber).HasMaxLength(100);
            b.Property(x => x.Pnr).HasMaxLength(100);
            b.Property(x => x.TicketFileName).HasMaxLength(255);
            b.Property(x => x.VisaFileName).HasMaxLength(255);
            b.Property(x => x.InsuranceFileName).HasMaxLength(255);
            b.HasIndex(x => x.TravelRequestId);
            b.HasIndex(x => x.EmployeeId);
        });

        builder.Entity<TravelRequestAllowanceDetail>(b =>
        {
            b.ToTable(TravelDbProperties.DbTablePrefix + "TravelRequestAllowanceDetails", TravelDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.EmployeeName).IsRequired().HasMaxLength(200);
            b.Property(x => x.EmployeeNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.RankName).HasMaxLength(200);
            b.Property(x => x.DailyRate).HasColumnType("decimal(18,2)");
            b.Property(x => x.AccommodationPaymentPercentage).HasColumnType("decimal(18,2)");
            b.Property(x => x.OverseasTotal).HasColumnType("decimal(18,2)");
            b.Property(x => x.ClothingAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.DeductionAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.NetTotal).HasColumnType("decimal(18,2)");
            b.Property(x => x.SegmentsJson).HasMaxLength(8000);
            b.Property(x => x.ClothingCalculationNote).HasMaxLength(2000);
            b.HasIndex(x => x.TravelRequestId);
            b.HasIndex(x => x.EmployeeId);
            b.HasIndex(x => x.EmployeeNumber);
            b.HasIndex(x => x.Category);
            b.HasIndex(x => x.CalculatedAt);
        });

        builder.Entity<AllowanceRule>(b =>
        {
            b.ToTable(TravelDbProperties.DbTablePrefix + "AllowanceRules", TravelDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Category);
            b.Property(x => x.AppliesWhenAccommodationIncluded);
            b.Property(x => x.AccommodationMultiplier).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.AllowanceType);
            b.HasIndex(x => x.TravelTypeDefinitionId);
            b.HasIndex(x => x.Category);
            b.HasOne<TravelTypeDefinition>().WithMany().HasForeignKey(x => x.TravelTypeDefinitionId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.AppliesWhenAccommodationIncluded);
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.Priority);

            b.HasMany(x => x.Segments).WithOne().HasForeignKey(s => s.AllowanceRuleId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AllowanceRuleSegment>(b =>
        {
            b.ToTable(TravelDbProperties.DbTablePrefix + "AllowanceRuleSegments", TravelDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Percentage).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.AllowanceRuleId);
            b.HasIndex(x => x.AppliesWhenTotalDaysFrom);
            b.HasIndex(x => x.AppliesWhenTotalDaysTo);
        });

        builder.Entity<AllowanceRate>(b =>
        {
            b.ToTable(TravelDbProperties.DbTablePrefix + "AllowanceRates", TravelDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            b.Property(x => x.AnnualPartialAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.Category).IsRequired();
            b.Property(x => x.TicketClass).IsRequired();
            b.HasIndex(x => x.RankId);
            b.HasIndex(x => x.Category);
            b.HasIndex(x => x.AllowanceType);
            b.HasIndex(x => x.TicketClass);
            b.HasIndex(x => x.IsActive);
        });

        builder.Entity<ClothingAllowanceRule>(b =>
        {
            b.ToTable(TravelDbProperties.DbTablePrefix + "ClothingAllowanceRules", TravelDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.FullAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.AnnualPartialAmount).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.Priority);

            b.HasMany(x => x.Ranks).WithOne().HasForeignKey(x => x.ClothingAllowanceRuleId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ClothingAllowanceRuleRank>(b =>
        {
            b.ToTable(TravelDbProperties.DbTablePrefix + "ClothingAllowanceRuleRanks", TravelDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.HasIndex(x => x.ClothingAllowanceRuleId);
            b.HasIndex(x => x.RankId);
            b.HasIndex(x => new { x.ClothingAllowanceRuleId, x.RankId }).IsUnique();
        });

        builder.Entity<EmployeeClothingHistory>(b =>
        {
            b.ToTable(TravelDbProperties.DbTablePrefix + "EmployeeClothingHistories", TravelDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.EmployeeId);
            b.HasIndex(x => x.TravelRequestId);
            b.HasIndex(x => x.ClothingAllowanceRuleId);
        });

        builder.Entity<TravelTypeDefinition>(b =>
        {
            b.ToTable(TravelDbProperties.DbTablePrefix + "TravelTypeDefinitions", TravelDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.HasIndex(x => x.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            b.HasIndex(x => x.IsActive);

        });

        builder.Entity<AccommodationRule>(b =>
        {
            b.ToTable(TravelDbProperties.DbTablePrefix + "AccommodationRules", TravelDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.PaymentPercentage).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.Priority);
        });

        builder.Entity<AccommodationRuleAllowanceRule>(b =>
        {
            b.ToTable(TravelDbProperties.DbTablePrefix + "AccommodationRuleAllowanceRules", TravelDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.HasIndex(x => x.AccommodationRuleId);
            b.HasIndex(x => x.AllowanceRuleId);
            b.HasIndex(x => new { x.AccommodationRuleId, x.AllowanceRuleId }).IsUnique();
        });
    }
}
