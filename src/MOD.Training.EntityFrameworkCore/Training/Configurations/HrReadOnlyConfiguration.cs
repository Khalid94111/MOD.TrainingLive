using Microsoft.EntityFrameworkCore;
using MOD.Training.Training.Hr;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace MOD.Training.Training.Configurations;

public static class HrReadOnlyConfiguration
{
    public static void ConfigureHrReadOnly(this ModelBuilder builder)
    {
        // Rank — read-only from HR module
        builder.Entity<Rank>(b =>
        {
            b.ToTable("HrRanks");        // was: b.ToTable("HrRanks", t => t.ExcludeFromMigrations());
            b.ConfigureByConvention();

            b.Property(x => x.NameAr).HasMaxLength(100);
            b.Property(x => x.NameEn).HasMaxLength(100);
            b.Property(x => x.PersonnelType).HasMaxLength(20);
        });

        // Employee — read-only from HR module
        builder.Entity<Employee>(b =>
        {
            b.ToTable("HrEmployees");    // was: b.ToTable("HrEmployees", t => t.ExcludeFromMigrations());
            b.ConfigureByConvention();

            b.Property(x => x.ServiceNumber).HasMaxLength(20);
            b.Property(x => x.FullNameAr).HasMaxLength(256);
            b.Property(x => x.FullNameEn).HasMaxLength(256);
            b.Property(x => x.Education).HasMaxLength(50);
            b.Property(x => x.Nationality).HasMaxLength(50);
            b.Property(x => x.SecurityClearance).HasMaxLength(50);
            b.Property(x => x.LanguageLevel).HasMaxLength(500);

            b.HasOne(x => x.Rank).WithMany().HasForeignKey(x => x.RankId).OnDelete(DeleteBehavior.NoAction);

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.ServiceNumber);
            b.HasIndex(x => x.MainUnitId);
        });
    }
}
