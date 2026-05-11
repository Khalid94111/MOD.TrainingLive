using System;

namespace MOD.Training.Training.DataSeeder;

/// <summary>
/// Fixed GUIDs for all seeded entities — shared across all sub-seeders.
/// </summary>
public static class RankIds
{
    public static readonly Guid Private = Guid.Parse("60000000-0000-0000-0000-000000000001");
    public static readonly Guid Corporal = Guid.Parse("60000000-0000-0000-0000-000000000002");
    public static readonly Guid Sergeant = Guid.Parse("60000000-0000-0000-0000-000000000003");
    public static readonly Guid StaffSergeant = Guid.Parse("60000000-0000-0000-0000-000000000004");
    public static readonly Guid SergeantMajor = Guid.Parse("60000000-0000-0000-0000-000000000005");
    public static readonly Guid SecondLieutenant = Guid.Parse("60000000-0000-0000-0000-000000000010");
    public static readonly Guid FirstLieutenant = Guid.Parse("60000000-0000-0000-0000-000000000011");
    public static readonly Guid Captain = Guid.Parse("60000000-0000-0000-0000-000000000012");
    public static readonly Guid Major = Guid.Parse("60000000-0000-0000-0000-000000000013");
    public static readonly Guid LtColonel = Guid.Parse("60000000-0000-0000-0000-000000000014");
    public static readonly Guid Colonel = Guid.Parse("60000000-0000-0000-0000-000000000015");
    public static readonly Guid Brigadier = Guid.Parse("60000000-0000-0000-0000-000000000016");
    public static readonly Guid MajorGeneral = Guid.Parse("60000000-0000-0000-0000-000000000017");
    public static readonly Guid LtGeneral = Guid.Parse("60000000-0000-0000-0000-000000000018");
}

public static class FieldIds
{
    public static readonly Guid Security = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid Leadership = Guid.Parse("20000000-0000-0000-0000-000000000002");
    public static readonly Guid Technology = Guid.Parse("20000000-0000-0000-0000-000000000003");
    public static readonly Guid Management = Guid.Parse("20000000-0000-0000-0000-000000000004");
    public static readonly Guid Engineering = Guid.Parse("20000000-0000-0000-0000-000000000005");
    public static readonly Guid Medical = Guid.Parse("20000000-0000-0000-0000-000000000006");
}

public static class CatalogIds
{
    public static readonly Guid Cybersecurity = Guid.Parse("30000000-0000-0000-0000-000000000001");
    public static readonly Guid Leadership = Guid.Parse("30000000-0000-0000-0000-000000000002");
    public static readonly Guid ProjectMgmt = Guid.Parse("30000000-0000-0000-0000-000000000003");
    public static readonly Guid NetworkEng = Guid.Parse("30000000-0000-0000-0000-000000000004");
    public static readonly Guid FirstAid = Guid.Parse("30000000-0000-0000-0000-000000000005");
    public static readonly Guid StrategicPlanning = Guid.Parse("30000000-0000-0000-0000-000000000006");
    public static readonly Guid DataAnalysis = Guid.Parse("30000000-0000-0000-0000-000000000007");
    public static readonly Guid FitnessInstructor = Guid.Parse("30000000-0000-0000-0000-000000000008");
    public static readonly Guid CombatEngineering = Guid.Parse("30000000-0000-0000-0000-000000000009");
    public static readonly Guid MilitaryWriting = Guid.Parse("30000000-0000-0000-0000-000000000010");
}

public static class FinancialItemIds
{
    // Parents
    public static readonly Guid CourseCost = Guid.Parse("40000000-0000-0000-0000-000000000001");
    public static readonly Guid Travel = Guid.Parse("40000000-0000-0000-0000-000000000002");
    public static readonly Guid Accommodation = Guid.Parse("40000000-0000-0000-0000-000000000003");
    // Children
    public static readonly Guid Tuition = Guid.Parse("40000000-0000-0000-0000-000000000011");
    public static readonly Guid Materials = Guid.Parse("40000000-0000-0000-0000-000000000012");
    public static readonly Guid Tickets = Guid.Parse("40000000-0000-0000-0000-000000000021");
    public static readonly Guid TravelAllowance = Guid.Parse("40000000-0000-0000-0000-000000000022");
    public static readonly Guid ClothingAllowance = Guid.Parse("40000000-0000-0000-0000-000000000023");
    public static readonly Guid Insurance = Guid.Parse("40000000-0000-0000-0000-000000000024");
    public static readonly Guid Visa = Guid.Parse("40000000-0000-0000-0000-000000000025");
    public static readonly Guid Hotel = Guid.Parse("40000000-0000-0000-0000-000000000031");
    public static readonly Guid Meals = Guid.Parse("40000000-0000-0000-0000-000000000032");
}

public static class ProviderIds
{
    public static readonly Guid SANS = Guid.Parse("50000000-0000-0000-0000-000000000001");
    public static readonly Guid PMI = Guid.Parse("50000000-0000-0000-0000-000000000002");
    public static readonly Guid CISCO = Guid.Parse("50000000-0000-0000-0000-000000000003");
    public static readonly Guid LocalAcademy = Guid.Parse("50000000-0000-0000-0000-000000000004");
}

public static class TenantIds
{
    public static readonly Guid COSSAF = Guid.Parse("e6b8587f-d179-fee8-e94e-3a209e449ddc");
    public static readonly Guid RAO = Guid.Parse("70e7984b-388a-9f5b-df3a-3a209e449f4c");
    public static readonly Guid RNO = Guid.Parse("7fae4701-c561-e270-bf05-3a209e449f7c");
}

/// <summary>
/// Phase 4B-α — stable GUIDs for HrGeographicalLocations (countries + their primary cities).
/// Pattern: countries use prefix 80000000-...001x, their cities use 80000000-...001x-NN.
/// </summary>
public static class GeographicalLocationIds
{
    // Countries
    public static readonly Guid Oman          = Guid.Parse("80000000-0000-0000-0000-000000000001");
    public static readonly Guid SaudiArabia   = Guid.Parse("80000000-0000-0000-0000-000000000002");
    public static readonly Guid UAE           = Guid.Parse("80000000-0000-0000-0000-000000000003");
    public static readonly Guid Kuwait        = Guid.Parse("80000000-0000-0000-0000-000000000004");
    public static readonly Guid Qatar         = Guid.Parse("80000000-0000-0000-0000-000000000005");
    public static readonly Guid Bahrain       = Guid.Parse("80000000-0000-0000-0000-000000000006");
    public static readonly Guid Egypt         = Guid.Parse("80000000-0000-0000-0000-000000000007");
    public static readonly Guid Jordan        = Guid.Parse("80000000-0000-0000-0000-000000000008");
    public static readonly Guid UnitedKingdom = Guid.Parse("80000000-0000-0000-0000-000000000009");
    public static readonly Guid USA           = Guid.Parse("80000000-0000-0000-0000-00000000000A");

    // Cities (one or two per country — minimal set for dev/QA cascade)
    public static readonly Guid Muscat        = Guid.Parse("80000000-0000-0000-0000-000000000101");
    public static readonly Guid Salalah       = Guid.Parse("80000000-0000-0000-0000-000000000102");
    public static readonly Guid Riyadh        = Guid.Parse("80000000-0000-0000-0000-000000000201");
    public static readonly Guid Jeddah        = Guid.Parse("80000000-0000-0000-0000-000000000202");
    public static readonly Guid Dubai         = Guid.Parse("80000000-0000-0000-0000-000000000301");
    public static readonly Guid AbuDhabi      = Guid.Parse("80000000-0000-0000-0000-000000000302");
    public static readonly Guid KuwaitCity    = Guid.Parse("80000000-0000-0000-0000-000000000401");
    public static readonly Guid Doha          = Guid.Parse("80000000-0000-0000-0000-000000000501");
    public static readonly Guid Manama        = Guid.Parse("80000000-0000-0000-0000-000000000601");
    public static readonly Guid Cairo         = Guid.Parse("80000000-0000-0000-0000-000000000701");
    public static readonly Guid Amman         = Guid.Parse("80000000-0000-0000-0000-000000000801");
    public static readonly Guid London        = Guid.Parse("80000000-0000-0000-0000-000000000901");
    public static readonly Guid Manchester    = Guid.Parse("80000000-0000-0000-0000-000000000902");
    public static readonly Guid Washington    = Guid.Parse("80000000-0000-0000-0000-000000000A01");
    public static readonly Guid NewYork       = Guid.Parse("80000000-0000-0000-0000-000000000A02");
}
