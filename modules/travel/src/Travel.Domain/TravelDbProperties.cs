namespace Travel;

public static class TravelDbProperties
{
    public static string DbTablePrefix { get; set; } = "Trv";

    public static string? DbSchema { get; set; } = null;

    public const string ConnectionStringName = "Travel";
}
