namespace RaceDay.Api.Dtos.Events;

public record EventSummaryResponse(
    int EventId,
    string Name,
    string? Description,
    string DisciplineType,
    DateOnly EventDate,
    TimeOnly? StartTime,
    string? Province,
    string? Venue,
    string Status,
    string? BannerImageUrl,
    string OrganiserName,
    decimal[] DistancesKm
);
