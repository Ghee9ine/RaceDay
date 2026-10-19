namespace RaceDay.Api.Models.Enums;

/// <summary>Matches the CK_Events_Status check constraint.</summary>
public static class EventStatuses
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";

    public static readonly IReadOnlyCollection<string> All = new[] { Draft, Published, Cancelled, Completed };

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
