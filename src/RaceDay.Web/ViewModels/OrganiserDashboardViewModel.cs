using RaceDay.Web.Models.Api;

namespace RaceDay.Web.ViewModels;

public class OrganiserDashboardViewModel
{
    public int TotalEvents { get; set; }
    public int PublishedEvents { get; set; }
    public int TotalEnrolments { get; set; }
    public List<MyEventDto> UpcomingEvents { get; set; } = new();
    public List<MyEventDto> RecentEvents { get; set; } = new();
}
