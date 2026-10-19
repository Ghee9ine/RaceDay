namespace RaceDay.Api.Dtos.Weather;

public record WeatherResponse(
    int WeatherSnapshotId,
    int EventId,
    DateTime RecordedAt,
    decimal? TemperatureC,
    decimal? WindSpeedKmh,
    string? ConditionDescription,
    string? Source
);
