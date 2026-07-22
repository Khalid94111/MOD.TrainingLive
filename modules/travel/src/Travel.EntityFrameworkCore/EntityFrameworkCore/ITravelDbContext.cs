using Microsoft.EntityFrameworkCore;
using Travel.Allowances;
using Travel.TravelRequests;
using Travel.TravelTypes;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Travel.EntityFrameworkCore;

[ConnectionStringName(TravelDbProperties.ConnectionStringName)]
public interface ITravelDbContext : IEfCoreDbContext
{
    DbSet<TravelRequest> TravelRequests { get; }
    DbSet<TravelRequestEmployee> TravelRequestEmployees { get; }
    DbSet<TravelDocument> TravelDocuments { get; }
    DbSet<TravelFlightOffer> TravelFlightOffers { get; }
    DbSet<TravelEmployeeDocument> TravelEmployeeDocuments { get; }
    DbSet<TravelRequestAllowanceDetail> TravelRequestAllowanceDetails { get; }
    DbSet<TravelFundingSourceVoteRule> TravelFundingSourceVoteRules { get; }
    DbSet<ClothingAllowanceRule> ClothingAllowanceRules { get; }
    DbSet<ClothingAllowanceRuleRank> ClothingAllowanceRuleRanks { get; }
    DbSet<TravelTypeDefinition> TravelTypeDefinitions { get; }
}
