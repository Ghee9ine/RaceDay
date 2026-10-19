namespace RaceDay.Api.Models.Enums;

/// <summary>Matches the CK_Results_Status check constraint.</summary>
public static class ResultStatuses
{
    public const string Finished = "Finished";
    public const string Dnf = "DNF";
    public const string Dsq = "DSQ";

    public static readonly IReadOnlyCollection<string> All = new[] { Finished, Dnf, Dsq };

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
