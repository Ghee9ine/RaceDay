using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services.Api;

namespace RaceDay.Web.Controllers.Organiser;

[RequireRole("Organiser")]
public class ResultsController : Controller
{
    private readonly IRaceDayApiClient _api;

    public ResultsController(IRaceDayApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var eventResult = await _api.GetEventAsync(eventId);
        if (!eventResult.Success)
        {
            return NotFound();
        }

        var enrolmentsResult = await _api.GetEnrolmentsForEventAsync(eventId);
        var enrolments = enrolmentsResult.Success ? enrolmentsResult.Data! : new List<OrganiserEnrolmentDto>();

        ViewBag.EventId = eventId;
        ViewBag.EventName = eventResult.Data!.Name;
        ViewBag.EventStatus = eventResult.Data.Status;
        ViewBag.Categories = eventResult.Data.Categories;
        return View(enrolments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Capture(int enrolmentId, int eventId, string? finishTime, int? position, string status)
    {
        var parsedTime = TimeOnly.TryParse(finishTime, out var t) ? t : (TimeOnly?)null;
        var result = await _api.CaptureResultAsync(enrolmentId, new CaptureResultRequestDto(parsedTime, position, status));

        TempData[result.Success ? "ToastSuccess" : "ToastError"] =
            result.Success ? "Result captured." : (result.ErrorMessage ?? "Couldn't capture that result.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int eventId)
    {
        var eventResult = await _api.GetEventAsync(eventId);
        if (!eventResult.Success)
        {
            return NotFound();
        }

        var dto = eventResult.Data!;
        var result = await _api.UpdateEventAsync(eventId, new UpdateEventRequestDto(
            dto.Name, dto.Description, dto.DisciplineType, dto.EventDate, dto.StartTime,
            dto.Province, dto.Venue, "Completed"));

        TempData[result.Success ? "ToastSuccess" : "ToastError"] =
            result.Success ? "Event marked as completed." : (result.ErrorMessage ?? "Couldn't update the event's status.");
        return RedirectToAction(nameof(Index), new { eventId });
    }
}
