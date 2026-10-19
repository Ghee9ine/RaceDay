namespace RaceDay.Api.Models;

/// <summary>
/// Junction row resolving the many-to-many relationship between
/// Participants (Users) and Categories. Cancelling is a soft delete
/// (Status set to Cancelled), so history and the unique
/// (ParticipantId, CategoryId) constraint both survive it.
/// </summary>
public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }
    public string? BibNumber { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public User Participant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}
