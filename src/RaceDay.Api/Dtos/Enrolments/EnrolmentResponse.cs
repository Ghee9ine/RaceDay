namespace RaceDay.Api.Dtos.Enrolments;

public record EnrolmentResponse(
    int EnrolmentId,
    int EventId,
    string EventName,
    int CategoryId,
    string CategoryName,
    string? BibNumber,
    DateTime EnrolmentDate,
    string Status
);
