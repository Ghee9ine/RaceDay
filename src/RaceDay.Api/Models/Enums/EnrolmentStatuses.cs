namespace RaceDay.Api.Models.Enums;

/// <summary>Matches the CK_Enrolments_Status check constraint.</summary>
public static class EnrolmentStatuses
{
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyCollection<string> All = new[] { Confirmed, Cancelled };
}
