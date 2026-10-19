using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Weather;

namespace RaceDay.Api.Services;

public class WeatherService : IWeatherService
{
    private readonly RaceDayDbContext _db;

    public WeatherService(RaceDayDbContext db)
    {
        _db = db;
    }

    public async Task<WeatherResponse?> GetLatestAsync(int eventId)
    {
        return await _db.WeatherSnapshots
            .AsNoTracking()
            .Where(w => w.EventId == eventId)
            .OrderByDescending(w => w.RecordedAt)
            .Select(w => new WeatherResponse(w.WeatherSnapshotId, w.EventId, w.RecordedAt, w.TemperatureC, w.WindSpeedKmh, w.ConditionDescription, w.Source))
            .FirstOrDefaultAsync();
    }
}
