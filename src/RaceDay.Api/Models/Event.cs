namespace RaceDay.Api.Models;

/// <summary>
/// An event owned by exactly one Organiser. Distance is deliberately not a
/// field here - one event can offer several distances - so it lives on
/// <see cref="Category"/> instead (see docs/api-endpoint-plan.md, Events
/// section, for the full design-decision note).
/// </summary>
public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DisciplineType { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public string? Province { get; set; }
    public string? Venue { get; set; }
    public string Status { get; set; } = string.Empty;

    /// <summary>Nullable, set only once uploaded in Part 3 (Azure Blob Storage).</summary>
    public string? BannerImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public User Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<WeatherSnapshot> WeatherSnapshots { get; set; } = new List<WeatherSnapshot>();
}
