using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services.Api;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers.Organiser;

[RequireRole("Organiser")]
public class CategoriesController : Controller
{
    private readonly IRaceDayApiClient _api;

    public CategoriesController(IRaceDayApiClient api)
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

        var categories = await _api.GetCategoriesForEventAsync(eventId);
        ViewBag.EventName = eventResult.Data!.Name;
        ViewBag.EventId = eventId;
        return View(categories.Success ? categories.Data : new List<CategoryDto>());
    }

    [HttpGet]
    public IActionResult Create(int eventId)
    {
        return View(new CategoryFormViewModel { EventId = eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _api.CreateCategoryAsync(model.EventId, ToRequestDto(model));
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Couldn't create that category.");
            return View(model);
        }

        TempData["ToastSuccess"] = "Category added.";
        return RedirectToAction(nameof(Index), new { eventId = model.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _api.GetCategoryAsync(id);
        if (!result.Success)
        {
            return NotFound();
        }

        var dto = result.Data!;
        return View(new CategoryFormViewModel
        {
            CategoryId = dto.CategoryId,
            EventId = dto.EventId,
            Name = dto.Name,
            DistanceKm = dto.DistanceKm,
            EntryFee = dto.EntryFee,
            MaxParticipants = dto.MaxParticipants,
            AgeGroup = dto.AgeGroup,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _api.UpdateCategoryAsync(id, ToRequestDto(model));
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Couldn't update that category.");
            return View(model);
        }

        TempData["ToastSuccess"] = "Category updated.";
        return RedirectToAction(nameof(Index), new { eventId = model.EventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var result = await _api.DeleteCategoryAsync(id);
        TempData[result.Success ? "ToastSuccess" : "ToastError"] =
            result.Success ? "Category removed." : (result.ErrorMessage ?? "Couldn't remove that category.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Route(int categoryId)
    {
        var categoryResult = await _api.GetCategoryAsync(categoryId);
        if (!categoryResult.Success)
        {
            return NotFound();
        }

        var routeResult = await _api.GetRouteAsync(categoryId);
        ViewBag.CategoryName = categoryResult.Data!.Name;
        ViewBag.EventId = categoryResult.Data.EventId;

        var model = routeResult.Success
            ? new RouteFormViewModel
            {
                CategoryId = categoryId,
                StartPoint = routeResult.Data!.StartPoint,
                EndPoint = routeResult.Data.EndPoint,
                ElevationGainM = routeResult.Data.ElevationGainM,
                MapUrl = routeResult.Data.MapUrl,
            }
            : new RouteFormViewModel { CategoryId = categoryId };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Route(RouteFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _api.UpsertRouteAsync(model.CategoryId,
            new UpsertRouteRequestDto(model.StartPoint, model.EndPoint, model.ElevationGainM, model.MapUrl));

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Couldn't save that route.");
            return View(model);
        }

        TempData["ToastSuccess"] = "Route saved.";
        return RedirectToAction(nameof(Route), new { categoryId = model.CategoryId });
    }

    private static CategoryRequestDto ToRequestDto(CategoryFormViewModel model) =>
        new(model.Name, model.DistanceKm, model.EntryFee, model.MaxParticipants, model.AgeGroup);
}
