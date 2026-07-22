using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Travel.Allowances;

namespace Travel.Ranks;

public class StaticRankLookup : IRankLookup
{
    private static readonly List<RankLookupDto> Ranks = new()
    {
        new RankLookupDto
        {
            Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
            Name = "عميد",
            Category = AllowanceCategory.A,
            Level = 1
        },
        new RankLookupDto
        {
            Id = Guid.Parse("a2222222-2222-2222-2222-222222222222"),
            Name = "عقيد",
            Category = AllowanceCategory.A,
            Level = 2
        },
        new RankLookupDto
        {
            Id = Guid.Parse("a3333333-3333-3333-3333-333333333333"),
            Name = "رائد",
            Category = AllowanceCategory.B,
            Level = 3
        },
        new RankLookupDto
        {
            Id = Guid.Parse("a4444444-4444-4444-4444-444444444444"),
            Name = "نقيب",
            Category = AllowanceCategory.B,
            Level = 4
        },
        new RankLookupDto
        {
            Id = Guid.Parse("a5555555-5555-5555-5555-555555555555"),
            Name = "ملازم أول",
            Category = AllowanceCategory.B,
            Level = 5
        },
        new RankLookupDto
        {
            Id = Guid.Parse("a6666666-6666-6666-6666-666666666666"),
            Name = "ملازم",
            Category = AllowanceCategory.B,
            Level = 6
        },
        new RankLookupDto
        {
            Id = Guid.Parse("a7777777-7777-7777-7777-777777777777"),
            Name = "رئيس أول",
            Category = AllowanceCategory.A,
            Level = 7
        },
        new RankLookupDto
        {
            Id = Guid.Parse("a8888888-8888-8888-8888-888888888888"),
            Name = "رئيس",
            Category = AllowanceCategory.A,
            Level = 8
        }
    };

    public Task<List<RankLookupDto>> GetRanksAsync()
    {
        return Task.FromResult(Ranks.ToList());
    }

    public Task<RankLookupDto?> GetRankByIdAsync(Guid id)
    {
        return Task.FromResult(Ranks.FirstOrDefault(r => r.Id == id));
    }
}
