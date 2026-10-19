namespace RaceDay.Api.Models;

/// <summary>One result per enrolment, captured by the owning organiser.</summary>
public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int CapturedByUserId { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }

    public Enrolment Enrolment { get; set; } = null!;
    public User CapturedBy { get; set; } = null!;
}
