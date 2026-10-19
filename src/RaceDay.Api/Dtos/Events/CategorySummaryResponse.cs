namespace RaceDay.Api.Dtos.Events;

public record CategorySummaryResponse(
    int CategoryId,
    string Name,
    decimal DistanceKm,
    decimal EntryFee,
    int? MaxParticipants,
    string? AgeGroup
);
