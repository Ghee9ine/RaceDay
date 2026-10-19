namespace RaceDay.Api.Dtos.Results;

public record LeaderboardEntryResponse(
    string? BibNumber,
    string ParticipantName,
    TimeOnly? FinishTime,
    int? Position,
    string Status
);
