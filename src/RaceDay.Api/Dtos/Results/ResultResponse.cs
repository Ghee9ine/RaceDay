namespace RaceDay.Api.Dtos.Results;

public record ResultResponse(
    int ResultId,
    int EnrolmentId,
    TimeOnly? FinishTime,
    int? Position,
    string Status,
    DateTime CapturedAt
);
