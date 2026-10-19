namespace RaceDay.Api.Models;

/// <summary>
/// The race route for one category (one-to-one). Maps to the Routes table;
/// named CourseRoute in C# to avoid colliding with
/// Microsoft.AspNetCore.Routing.Route, which the Web SDK brings in via an
/// implicit global using.
/// </summary>
public class CourseRoute
{
    public int RouteId { get; set; }
    public int CategoryId { get; set; }
    public string StartPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
    public int? ElevationGainM { get; set; }
    public string? MapUrl { get; set; }

    public Category Category { get; set; } = null!;
}
