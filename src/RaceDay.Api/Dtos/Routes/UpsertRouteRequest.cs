using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos.Routes;

public record UpsertRouteRequest(
    [Required, MaxLength(200)] string StartPoint,
    [Required, MaxLength(200)] string EndPoint,
    [Range(0, 100000)] int? ElevationGainM,
    [MaxLength(300)] string? MapUrl
);
