using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services.Api;

namespace RaceDay.Web.Controllers.Organiser;

[RequireRole("Organiser")]
public class EnrolmentsController : Controller
{
    private readonly IRaceDayApiClient _api;

    public EnrolmentsController(IRaceDayApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int eventId, int? categoryId)
    {
        var eventResult = await _api.GetEventAsync(eventId);
        if (!eventResult.Success)
        {
            return NotFound();
        }

        var enrolmentsResult = await _api.GetEnrolmentsForEventAsync(eventId);
        var enrolments = enrolmentsResult.Success ? enrolmentsResult.Data! : new List<OrganiserEnrolmentDto>();

        if (categoryId is not null)
        {
            enrolments = enrolments.Where(e => e.CategoryId == categoryId).ToList();
        }

        ViewBag.EventId = eventId;
        ViewBag.EventName = eventResult.Data!.Name;
        ViewBag.Categories = eventResult.Data.Categories;
        ViewBag.SelectedCategoryId = categoryId;
        return View(enrolments);
    }
}
