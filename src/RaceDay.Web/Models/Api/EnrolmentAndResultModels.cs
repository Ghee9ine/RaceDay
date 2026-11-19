namespace RaceDay.Web.Models.Api;

public record EnrolmentDto(int EnrolmentId, int EventId, string EventName, int CategoryId, string CategoryName, string? BibNumber, DateTime EnrolmentDate, string Status);

public record OrganiserEnrolmentDto(
    int EnrolmentId, int EventId, string EventName, int CategoryId, string CategoryName, string? BibNumber,
    DateTime EnrolmentDate, string Status, string ParticipantName, string ParticipantEmail);

public record CaptureResultRequestDto(TimeOnly? FinishTime, int? Position, string Status);
public record ResultDto(int ResultId, int EnrolmentId, TimeOnly? FinishTime, int? Position, string Status, DateTime CapturedAt);

public record MyResultDto(string EventName, DateOnly EventDate, string CategoryName, decimal DistanceKm, TimeOnly? FinishTime, int? Position, int TotalFinishers, string Status);

public record LeaderboardEntryDto(string? BibNumber, string ParticipantName, TimeOnly? FinishTime, int? Position, string Status);
