namespace RaceDay.Api.Models;

/// <summary>A recorded forecast for an event. Many per event over time.</summary>
public class WeatherSnapshot
{
    public int WeatherSnapshotId { get; set; }
    public int EventId { get; set; }
    public DateTime RecordedAt { get; set; }
    public decimal? TemperatureC { get; set; }
    public decimal? WindSpeedKmh { get; set; }
    public string? ConditionDescription { get; set; }
    public string? Source { get; set; }

    public Event Event { get; set; } = null!;
}
