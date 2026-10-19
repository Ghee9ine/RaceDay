namespace RaceDay.Api.Dtos.Events;

/// <summary>An organiser's own event, in any status, with an enrolment count for a dashboard.</summary>
public record MyEventResponse(
    int EventId,
    string Name,
    string DisciplineType,
    DateOnly EventDate,
    string Status,
    int TotalEnrolments
);
