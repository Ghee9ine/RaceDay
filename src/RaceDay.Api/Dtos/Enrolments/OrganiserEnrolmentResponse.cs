namespace RaceDay.Api.Dtos.Enrolments;

/// <summary>An enrolment as seen by the organiser managing race-day logistics - adds who the participant is.</summary>
public record OrganiserEnrolmentResponse(
    int EnrolmentId,
    int EventId,
    string EventName,
    int CategoryId,
    string CategoryName,
    string? BibNumber,
    DateTime EnrolmentDate,
    string Status,
    string ParticipantName,
    string ParticipantEmail
);
