using RaceDay.Api.Dtos.Weather;

namespace RaceDay.Api.Services;

public interface IWeatherService
{
    /// <summary>The most recently recorded snapshot for an event, or null if the event
    /// doesn't exist or has no recorded forecast yet - either way, the controller 404s.</summary>
    Task<WeatherResponse?> GetLatestAsync(int eventId);
}
