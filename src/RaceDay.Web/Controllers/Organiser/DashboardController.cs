using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services.Api;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers.Organiser;

[RequireRole("Organiser")]
public class DashboardController : Controller
{
    private readonly IRaceDayApiClient _api;

    public DashboardController(IRaceDayApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyEventsAsync();
        var events = result.Success ? result.Data! : new List<Models.Api.MyEventDto>();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var vm = new OrganiserDashboardViewModel
        {
            TotalEvents = events.Count,
            PublishedEvents = events.Count(e => e.Status == "Published"),
            UpcomingEvents = events.Where(e => e.EventDate >= today && e.Status == "Published")
                .OrderBy(e => e.EventDate).Take(5).ToList(),
            TotalEnrolments = events.Sum(e => e.TotalEnrolments),
            RecentEvents = events.OrderByDescending(e => e.EventDate).Take(10).ToList(),
        };

        return View(vm);
    }
}
