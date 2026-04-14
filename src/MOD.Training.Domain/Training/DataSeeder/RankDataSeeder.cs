using MOD.Training.Training.Hr;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.DataSeeder;

public interface IRankDataSeeder
{
    Task SeedAsync(DataSeedContext context);
}

public class RankDataSeeder(
    IRepository<Rank, Guid> rankRepo,
    ICurrentTenant currentTenant)
    : ITransientDependency, IRankDataSeeder
{
    public async Task SeedAsync(DataSeedContext context)
    {
        using (currentTenant.Change(null))
        {
            if (await rankRepo.AnyAsync(x => x.Id == RankIds.Captain))
                return;

            await rankRepo.InsertManyAsync(new List<Rank>
            {
                new Rank(RankIds.Private){ NameAr = "جندي", NameEn = "Private", SortOrder = 1, PersonnelType = "Enlisted", IsActive = true },
                new Rank(RankIds.Corporal){ NameAr = "عريف", NameEn = "Corporal", SortOrder = 2, PersonnelType = "Enlisted", IsActive = true },
                new Rank(RankIds.Sergeant){ NameAr = "رقيب", NameEn = "Sergeant", SortOrder = 3, PersonnelType = "Enlisted", IsActive = true },
                new Rank(RankIds.StaffSergeant){ NameAr = "رقيب أول", NameEn = "Staff Sergeant", SortOrder = 4, PersonnelType = "Enlisted", IsActive = true },
                new Rank(RankIds.SergeantMajor){ NameAr = "رئيس عرفاء", NameEn = "Sergeant Major", SortOrder = 5, PersonnelType = "Enlisted", IsActive = true },
                new Rank(RankIds.SecondLieutenant){ NameAr = "ملازم", NameEn = "Second Lieutenant", SortOrder = 10, PersonnelType = "Officer", IsActive = true },
                new Rank(RankIds.FirstLieutenant){ NameAr = "ملازم أول", NameEn = "First Lieutenant", SortOrder = 11, PersonnelType = "Officer", IsActive = true },
                new Rank(RankIds.Captain){ NameAr = "نقيب", NameEn = "Captain", SortOrder = 12, PersonnelType = "Officer", IsActive = true },
                new Rank(RankIds.Major){ NameAr = "رائد", NameEn = "Major", SortOrder = 13, PersonnelType = "Officer", IsActive = true },
                new Rank(RankIds.LtColonel){ NameAr = "مقدم", NameEn = "Lieutenant Colonel", SortOrder = 14, PersonnelType = "Officer", IsActive = true },
                new Rank(RankIds.Colonel) {NameAr = "عقيد", NameEn = "Colonel", SortOrder = 15, PersonnelType = "Officer", IsActive = true },
                new Rank(RankIds.Brigadier){ NameAr = "عميد", NameEn = "Brigadier", SortOrder = 16, PersonnelType = "Officer", IsActive = true },
                new Rank(RankIds.MajorGeneral){ NameAr = "لواء", NameEn = "Major General", SortOrder = 17, PersonnelType = "Officer", IsActive = true },
                new Rank(RankIds.LtGeneral){ NameAr = "فريق", NameEn = "Lieutenant General", SortOrder = 18, PersonnelType = "Officer", IsActive = true },
            }, autoSave: true);
        }
    }
}
