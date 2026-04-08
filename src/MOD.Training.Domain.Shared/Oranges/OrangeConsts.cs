namespace MOD.Training.Oranges;

public static class OrangeConsts
{
    private const string DefaultSorting = "{0}CreationTime desc";

    public static string GetDefaultSorting(bool withEntityName)
    {
        return string.Format(DefaultSorting, withEntityName ? "Orange." : string.Empty);
    }
}