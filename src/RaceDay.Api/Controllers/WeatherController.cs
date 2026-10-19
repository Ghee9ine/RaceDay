using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>The most recently recorded weather forecast for an event.</summary>
[ApiController]
[Tags("Race-Day Preparation")]
[Authorize]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public WeatherController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    /// <summary>Returns the most recent weather snapshot recorded for an event's date/location.
    /// Reads a recorded WeatherSnapshot; this does not call a live weather API (see README,
    /// known scope decision).</summary>
    /// <response code="200">The latest weather snapshot.</response>
    /// <response code="401">No active session.</response>
    /// <response code="404">No forecast recorded yet for this event.</response>
    [HttpGet("api/events/{eventId:int}/weather")]
    [ProducesResponseType(typeof(Dtos.Weather.WeatherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatest(int eventId)
    {
        var weather = await _weatherService.GetLatestAsync(eventId);
        return weather is null ? NotFound() : Ok(weather);
    }
}
