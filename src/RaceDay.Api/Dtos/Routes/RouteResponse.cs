namespace RaceDay.Api.Dtos.Routes;

public record RouteResponse(
    int RouteId,
    int CategoryId,
    string StartPoint,
    string EndPoint,
    int? ElevationGainM,
    string? MapUrl
);
