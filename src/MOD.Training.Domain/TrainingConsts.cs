using Volo.Abp.Identity;

namespace MOD.Training;

public static class TrainingConsts
{
    public const string DbTablePrefix = "App";
    public const string? DbSchema = null;
    public const string AdminEmailDefaultValue = IdentityDataSeedContributor.AdminEmailDefaultValue;
    public const string AdminPasswordDefaultValue = "1q2w3E*";
  

    // Course Catalog
    public const int MaxCourseNameLength = 256;
    public const int MaxDescriptionLength = 500;

    // Course Fields
    public const int MaxFieldNameLength = 256;

    // Conditions
    public const int MaxConditionValueLength = 1000;

    // Proposals
    public const int MaxRejectionReasonLength = 500;

    // Countries
    public const int MaxCountryNameLength = 128;
    public const int MaxCountryCodeLength = 3;

    // General
    public const int MaxCategoryLength = 50;
    public const int MaxNatureLength = 50;
    public const int MaxNotesLength = 2000;
    public const int MaxFundingSourceLength = 256;
    public const int MaxJustificationLength = 1000;
    public const int MaxLocationLength = 256;
    public const int MaxSessionCodeLength = 50;
    public const int MaxCodeLength = 50;
 

    // === Phase 3: Providers ===
    public const int MaxProviderNameLength = 256;
    public const int MaxContactPersonLength = 128;
    public const int MaxEmailLength = 256;
    public const int MaxPhoneLength = 50;
    public const int MaxAddressLength = 500;
    public const int MaxWebsiteLength = 256;
    public const int MaxObjectivesLength = 256;
}
