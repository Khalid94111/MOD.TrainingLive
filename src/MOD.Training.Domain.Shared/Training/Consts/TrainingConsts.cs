namespace MOD.Training.Training.Consts;

public static class TrainingConsts
{
    /// <summary>
    /// DB table prefix for all Training module tables.
    /// </summary>
    public const string DbTablePrefix = "Trn";
    public const string? DbSchema = null;

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
}
