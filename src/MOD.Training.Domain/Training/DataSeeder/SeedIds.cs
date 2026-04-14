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
