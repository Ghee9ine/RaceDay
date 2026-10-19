namespace RaceDay.Api.Models.Enums;

/// <summary>Matches the CK_Events_DisciplineType check constraint.</summary>
public static class DisciplineTypes
{
    public const string Running = "Running";
    public const string Cycling = "Cycling";
    public const string Walking = "Walking";

    public static readonly IReadOnlyCollection<string> All = new[] { Running, Cycling, Walking };

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
