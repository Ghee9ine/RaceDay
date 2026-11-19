using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services.Api;

namespace RaceDay.Web.Controllers;

[RequireRole("Participant")]
public class MyResultsController : Controller
{
    private readonly IRaceDayApiClient _api;

    public MyResultsController(IRaceDayApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyResultsAsync();
        return View(result.Success ? result.Data : new List<Models.Api.MyResultDto>());
    }
}
