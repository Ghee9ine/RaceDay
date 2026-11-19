namespace RaceDay.Web.Models.Api;

public record CategorySummaryDto(int CategoryId, string Name, decimal DistanceKm, decimal EntryFee, int? MaxParticipants, string? AgeGroup);

public record EventSummaryDto(
    int EventId, string Name, string? Description, string DisciplineType, DateOnly EventDate, TimeOnly? StartTime,
    string? Province, string? Venue, string Status, string? BannerImageUrl, string OrganiserName, decimal[] DistancesKm);

public record EventDetailDto(
    int EventId, string Name, string? Description, string DisciplineType, DateOnly EventDate, TimeOnly? StartTime,
    string? Province, string? Venue, string Status, string? BannerImageUrl, string OrganiserName,
    decimal[] DistancesKm, CategorySummaryDto[] Categories);

public record MyEventDto(int EventId, string Name, string DisciplineType, DateOnly EventDate, string Status, int TotalEnrolments);

public record CreateEventRequestDto(
    string Name, string? Description, string DisciplineType, DateOnly EventDate, TimeOnly? StartTime,
    string? Province, string? Venue, string? Status);

public record UpdateEventRequestDto(
    string Name, string? Description, string DisciplineType, DateOnly EventDate, TimeOnly? StartTime,
    string? Province, string? Venue, string Status);

public record BannerUploadedDto(string BannerImageUrl);

public record CategoryRequestDto(string Name, decimal DistanceKm, decimal EntryFee, int? MaxParticipants, string? AgeGroup);
public record CategoryDto(int CategoryId, int EventId, string Name, decimal DistanceKm, decimal EntryFee, int? MaxParticipants, string? AgeGroup);

public record RouteDto(int RouteId, int CategoryId, string StartPoint, string EndPoint, int? ElevationGainM, string? MapUrl);
public record UpsertRouteRequestDto(string StartPoint, string EndPoint, int? ElevationGainM, string? MapUrl);

public record WeatherDto(int WeatherSnapshotId, int EventId, DateTime RecordedAt, decimal? TemperatureC, decimal? WindSpeedKmh, string? ConditionDescription, string? Source);
