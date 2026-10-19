namespace RaceDay.Api.Models;

/// <summary>
/// An account in the system. Holds both Organisers and Participants,
/// distinguished by <see cref="Role"/> (see Models.Enums.Roles).
/// </summary>
public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    /// <summary>Nullable, set only once uploaded in Part 3 (Azure Blob Storage).</summary>
    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    public ICollection<Result> CapturedResults { get; set; } = new List<Result>();
}
