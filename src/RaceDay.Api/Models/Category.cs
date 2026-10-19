namespace RaceDay.Api.Models;

/// <summary>
/// A distance and/or age category offered under one event (e.g. "21km Half
/// Marathon" or an age group such as "Under 20"). <see cref="AgeGroup"/> is
/// separate from distance so both kinds of category fit without overloading
/// one field.
/// </summary>
public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int? MaxParticipants { get; set; }
    public string? AgeGroup { get; set; }

    public Event Event { get; set; } = null!;

    // Named CourseRoute, not Route: "Route" collides with the ASP.NET Core
    // Web SDK's implicit `using Microsoft.AspNetCore.Routing;`, which
    // exposes its own public Microsoft.AspNetCore.Routing.Route class. The
    // database table is still named Routes (see Configurations/RouteConfiguration.cs).
    public CourseRoute? CourseRoute { get; set; }

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
