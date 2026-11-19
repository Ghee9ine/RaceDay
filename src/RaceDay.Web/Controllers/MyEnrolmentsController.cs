using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services.Api;

namespace RaceDay.Web.Controllers;

[RequireRole("Participant")]
public class MyEnrolmentsController : Controller
{
    private readonly IRaceDayApiClient _api;

    public MyEnrolmentsController(IRaceDayApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyEnrolmentsAsync();
        return View(result.Success ? result.Data : new List<Models.Api.EnrolmentDto>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _api.CancelEnrolmentAsync(id);
        TempData[result.Success ? "ToastSuccess" : "ToastError"] =
            result.Success ? "Your enrolment has been cancelled." : (result.ErrorMessage ?? "Couldn't cancel that enrolment.");
        return RedirectToAction(nameof(Index));
    }
}
