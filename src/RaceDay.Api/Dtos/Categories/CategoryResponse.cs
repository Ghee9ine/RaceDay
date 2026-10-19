namespace RaceDay.Api.Dtos.Categories;

public record CategoryResponse(
    int CategoryId,
    int EventId,
    string Name,
    decimal DistanceKm,
    decimal EntryFee,
    int? MaxParticipants,
    string? AgeGroup
);
