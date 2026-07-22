using Microsoft.EntityFrameworkCore;
using Travel.Allowances;
using Travel.TravelRequests;
using Travel.TravelTypes;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Travel.EntityFrameworkCore;

[ConnectionStringName(TravelDbProperties.ConnectionStringName)]
public class TravelDbContext : AbpDbContext<TravelDbContext>, ITravelDbContext
{
    public DbSet<TravelRequest> TravelRequests { get; set; } = null!;
    public DbSet<TravelRequestEmployee> TravelRequestEmployees { get; set; } = null!;
    public DbSet<TravelDocument> TravelDocuments { get; set; } = null!;
    public DbSet<TravelFlightOffer> TravelFlightOffers { get; set; } = null!;
    public DbSet<TravelEmployeeDocument> TravelEmployeeDocuments { get; set; } = null!;
    public DbSet<TravelRequestAllowanceDetail> TravelRequestAllowanceDetails { get; set; } = null!;
    public DbSet<TravelFundingSourceVoteRule> TravelFundingSourceVoteRules { get; set; } = null!;
    public DbSet<AllowanceRule> AllowanceRules { get; set; } = null!;
    public DbSet<AllowanceRuleSegment> AllowanceRuleSegments { get; set; } = null!;
    public DbSet<AllowanceRate> AllowanceRates { get; set; } = null!;
    public DbSet<ClothingAllowanceRule> ClothingAllowanceRules { get; set; } = null!;
    public DbSet<ClothingAllowanceRuleRank> ClothingAllowanceRuleRanks { get; set; } = null!;
    public DbSet<EmployeeClothingHistory> EmployeeClothingHistories { get; set; } = null!;
    public DbSet<TravelTypeDefinition> TravelTypeDefinitions { get; set; } = null!;
    public DbSet<AccommodationRule> AccommodationRules { get; set; } = null!;
    public DbSet<AccommodationRuleAllowanceRule> AccommodationRuleAllowanceRules { get; set; } = null!;

    public TravelDbContext(DbContextOptions<TravelDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Owned<AllowanceSnapshot>();

        base.OnModelCreating(builder);

        builder.ConfigureTravelManagement();
    }
}
