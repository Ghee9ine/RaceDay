namespace RaceDay.Api.Models.Enums;

/// <summary>
/// The two account roles the brief defines. Stored as plain strings (matching
/// the CK_Users_Role check constraint) rather than a numeric enum, so the
/// database value is self-describing without an EF value converter.
/// </summary>
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static readonly IReadOnlyCollection<string> All = new[] { Organiser, Participant };

    public static bool IsValid(string? role) => role is not null && All.Contains(role);
}
