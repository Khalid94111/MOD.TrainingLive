using MOD.Training.Training.Hr;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.DataSeeder;

public interface IGeographicalLocationDataSeeder
{
    Task SeedAsync(DataSeedContext context);
}

/// <summary>
/// Phase 4B-α — seeds HrGeographicalLocations (master data, supra-tenant).
/// Minimal set: Oman + GCC + a few common training destinations, with one or two
/// cities each. Sufficient to drive the Country/City cascade dropdowns on PAGE 4.8
/// (Price Quotes) and the new TrainingProvider form. Idempotent — checks existence
/// before inserting.
/// </summary>
public class GeographicalLocationDataSeeder(
    IRepository<GeographicalLocation, Guid> repo,
    ICurrentTenant currentTenant)
    : ITransientDependency, IGeographicalLocationDataSeeder
{
    public async Task SeedAsync(DataSeedContext context)
    {
        using (currentTenant.Change(null))
        {
            if (await repo.AnyAsync(x => x.Id == GeographicalLocationIds.Oman))
                return;

            var rows = new List<GeographicalLocation>
            {
                // Countries (LocationParentId = null)
                Country(GeographicalLocationIds.Oman,          "عُمان",            "Oman"),
                Country(GeographicalLocationIds.SaudiArabia,   "المملكة العربية السعودية", "Saudi Arabia"),
                Country(GeographicalLocationIds.UAE,           "الإمارات",         "United Arab Emirates"),
                Country(GeographicalLocationIds.Kuwait,        "الكويت",           "Kuwait"),
                Country(GeographicalLocationIds.Qatar,         "قطر",              "Qatar"),
                Country(GeographicalLocationIds.Bahrain,       "البحرين",          "Bahrain"),
                Country(GeographicalLocationIds.Egypt,         "مصر",              "Egypt"),
                Country(GeographicalLocationIds.Jordan,        "الأردن",           "Jordan"),
                Country(GeographicalLocationIds.UnitedKingdom, "المملكة المتحدة",  "United Kingdom"),
                Country(GeographicalLocationIds.USA,           "الولايات المتحدة", "United States"),

                // Cities
                City(GeographicalLocationIds.Muscat,     GeographicalLocationIds.Oman,          "مسقط",       "Muscat"),
                City(GeographicalLocationIds.Salalah,    GeographicalLocationIds.Oman,          "صلالة",      "Salalah"),
                City(GeographicalLocationIds.Riyadh,     GeographicalLocationIds.SaudiArabia,   "الرياض",     "Riyadh"),
                City(GeographicalLocationIds.Jeddah,     GeographicalLocationIds.SaudiArabia,   "جدة",        "Jeddah"),
                City(GeographicalLocationIds.Dubai,      GeographicalLocationIds.UAE,           "دبي",        "Dubai"),
                City(GeographicalLocationIds.AbuDhabi,   GeographicalLocationIds.UAE,           "أبوظبي",     "Abu Dhabi"),
                City(GeographicalLocationIds.KuwaitCity, GeographicalLocationIds.Kuwait,        "مدينة الكويت", "Kuwait City"),
                City(GeographicalLocationIds.Doha,       GeographicalLocationIds.Qatar,         "الدوحة",     "Doha"),
                City(GeographicalLocationIds.Manama,     GeographicalLocationIds.Bahrain,       "المنامة",    "Manama"),
                City(GeographicalLocationIds.Cairo,      GeographicalLocationIds.Egypt,         "القاهرة",    "Cairo"),
                City(GeographicalLocationIds.Amman,      GeographicalLocationIds.Jordan,        "عمّان",      "Amman"),
                City(GeographicalLocationIds.London,     GeographicalLocationIds.UnitedKingdom, "لندن",       "London"),
                City(GeographicalLocationIds.Manchester, GeographicalLocationIds.UnitedKingdom, "مانشستر",    "Manchester"),
                City(GeographicalLocationIds.Washington, GeographicalLocationIds.USA,           "واشنطن",     "Washington"),
                City(GeographicalLocationIds.NewYork,    GeographicalLocationIds.USA,           "نيويورك",    "New York"),
            };

            await repo.InsertManyAsync(rows, autoSave: true);
        }
    }

    private static GeographicalLocation Country(Guid id, string ar, string en)
        => new(id) { ArabicName = ar, EnglishName = en, LocationParentId = null };

    private static GeographicalLocation City(Guid id, Guid countryId, string ar, string en)
        => new(id) { ArabicName = ar, EnglishName = en, LocationParentId = countryId };
}
