using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.ViewModels;

public static class SouthAfricanProvinces
{
    public static readonly string[] All =
    {
        "Eastern Cape", "Free State", "Gauteng", "KwaZulu-Natal", "Limpopo",
        "Mpumalanga", "Northern Cape", "North West", "Western Cape",
    };
}

public static class DisciplineOptions
{
    public static readonly string[] All = { "Running", "Cycling", "Walking" };
}

public class EventFilterViewModel
{
    public string? Province { get; set; }
    public string? DisciplineType { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required, Display(Name = "Event type")]
    public string DisciplineType { get; set; } = "Running";

    [Required, Display(Name = "Event date"), DataType(DataType.Date)]
    public DateOnly EventDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(30));

    [Display(Name = "Start time"), DataType(DataType.Time)]
    public TimeOnly? StartTime { get; set; }

    public string? Province { get; set; }

    [MaxLength(200)]
    public string? Venue { get; set; }

    [Required]
    public string Status { get; set; } = "Published";

    public bool IsEdit => EventId is not null;

    // Only used on Create: the event's first category is derived from these.
    [Range(0.01, 10000), Display(Name = "Main distance (km)")]
    public decimal? MainDistanceKm { get; set; } = 10m;

    [MaxLength(100), Display(Name = "Category name")]
    public string? CategoryName { get; set; }

    public decimal[]? ExistingDistancesKm { get; set; }

    public IFormFile? Banner { get; set; }
}

public class CategoryFormViewModel
{
    public int EventId { get; set; }
    public int? CategoryId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, Range(0.01, 10000), Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Range(0, 100000), Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }

    [Range(1, int.MaxValue), Display(Name = "Max participants")]
    public int? MaxParticipants { get; set; }

    [Display(Name = "Age group")]
    public string? AgeGroup { get; set; }

    public bool IsEdit => CategoryId is not null;
}

public static class AgeGroupOptions
{
    public static readonly string[] All = { "Open", "Under 20", "Senior", "Veteran", "Master" };
}

public class RouteFormViewModel
{
    public int CategoryId { get; set; }

    [Required, MaxLength(200), Display(Name = "Start point")]
    public string StartPoint { get; set; } = string.Empty;

    [Required, MaxLength(200), Display(Name = "End point")]
    public string EndPoint { get; set; } = string.Empty;

    [Range(0, 100000), Display(Name = "Elevation gain (m)")]
    public int? ElevationGainM { get; set; }

    [MaxLength(300), Display(Name = "Map URL")]
    public string? MapUrl { get; set; }
}
