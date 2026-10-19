namespace RaceDay.Api.Dtos.Results;

/// <summary>Supports showing e.g. "47th out of 312 finishers" without the client computing it.</summary>
public record MyResultResponse(
    string EventName,
    DateOnly EventDate,
    string CategoryName,
    decimal DistanceKm,
    TimeOnly? FinishTime,
    int? Position,
    int TotalFinishers,
    string Status
);
