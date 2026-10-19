using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos.Categories;

// Shared shape for both create and update - the plan gives them identical
// bodies, so one request type covers both endpoints.
public record CategoryRequest(
    [Required, MaxLength(100)] string Name,
    [Required, Range(0.01, 10000)] decimal DistanceKm,
    [Range(0, 100000)] decimal EntryFee,
    [Range(1, int.MaxValue)] int? MaxParticipants,
    [MaxLength(30)] string? AgeGroup
);
