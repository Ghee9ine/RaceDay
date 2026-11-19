using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services.Api;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

public class EventsController : Controller
{
    private readonly IRaceDayApiClient _api;

    public EventsController(IRaceDayApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(EventFilterViewModel filter)
    {
        var result = await _api.GetPublishedEventsAsync(filter.Province, filter.DisciplineType, filter.From, filter.To);
        ViewBag.Filter = filter;
        return View(result.Success ? result.Data : new List<Models.Api.EventSummaryDto>());
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _api.GetEventAsync(id);
        if (!result.Success)
        {
            return NotFound();
        }

        var weather = await _api.GetWeatherAsync(id);
        ViewBag.Weather = weather.Success ? weather.Data : null;

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireRole("Participant")]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var result = await _api.EnrolAsync(categoryId);
        if (result.Success)
        {
            TempData["ToastSuccess"] = "You're enrolled! Check My Enrolments for your bib and status.";
        }
        else
        {
            TempData["ToastError"] = result.ErrorMessage ?? "Couldn't complete your enrolment. Please try again.";
        }

        return RedirectToAction(nameof(Details), new { id = eventId });
    }
}
