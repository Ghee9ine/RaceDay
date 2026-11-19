using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services.Api;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers.Organiser;

[RequireRole("Organiser")]
public class OrganiserEventsController : Controller
{
    private readonly IRaceDayApiClient _api;

    public OrganiserEventsController(IRaceDayApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyEventsAsync();
        return View(result.Success ? result.Data : new List<MyEventDto>());
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new EventFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var createResult = await _api.CreateEventAsync(new CreateEventRequestDto(
            model.Name, model.Description, model.DisciplineType, model.EventDate, model.StartTime,
            model.Province, model.Venue, model.Status));

        if (!createResult.Success)
        {
            ApplyApiErrors(createResult);
            return View(model);
        }

        var eventId = createResult.Data!.EventId;

        // Every event needs at least one category to be enrollable - the
        // create form captures its first category's name/distance inline
        // so an organiser doesn't have to visit a second screen just to
        // make a brand-new event usable.
        var categoryName = string.IsNullOrWhiteSpace(model.CategoryName) ? $"{model.MainDistanceKm:0.#}km" : model.CategoryName!;
        if (model.MainDistanceKm is not null)
        {
            await _api.CreateCategoryAsync(eventId, new CategoryRequestDto(categoryName, model.MainDistanceKm.Value, 0, null, null));
        }

        if (model.Banner is not null && model.Banner.Length > 0)
        {
            var bannerResult = await _api.UploadBannerAsync(eventId, model.Banner);
            if (!bannerResult.Success)
            {
                TempData["ToastWarning"] = $"Event created, but the banner couldn't be uploaded: {bannerResult.ErrorMessage}";
                return RedirectToAction(nameof(Edit), new { id = eventId });
            }
        }

        TempData["ToastSuccess"] = "Event created.";
        return RedirectToAction(nameof(Edit), new { id = eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _api.GetEventAsync(id);
        if (!result.Success)
        {
            return NotFound();
        }

        return View(ToFormViewModel(result.Data!));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var updateResult = await _api.UpdateEventAsync(id, new UpdateEventRequestDto(
            model.Name, model.Description, model.DisciplineType, model.EventDate, model.StartTime,
            model.Province, model.Venue, model.Status));

        if (!updateResult.Success)
        {
            ApplyApiErrors(updateResult);
            return View(model);
        }

        if (model.Banner is not null && model.Banner.Length > 0)
        {
            var bannerResult = await _api.UploadBannerAsync(id, model.Banner);
            if (!bannerResult.Success)
            {
                TempData["ToastWarning"] = $"Event saved, but the banner couldn't be uploaded: {bannerResult.ErrorMessage}";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        TempData["ToastSuccess"] = "Event updated.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _api.DeleteEventAsync(id);
        TempData[result.Success ? "ToastSuccess" : "ToastError"] =
            result.Success ? "Event deleted." : (result.ErrorMessage ?? "Couldn't delete that event.");
        return RedirectToAction(nameof(Index));
    }

    private static EventFormViewModel ToFormViewModel(EventDetailDto dto) => new()
    {
        EventId = dto.EventId,
        Name = dto.Name,
        Description = dto.Description,
        DisciplineType = dto.DisciplineType,
        EventDate = dto.EventDate,
        StartTime = dto.StartTime,
        Province = dto.Province,
        Venue = dto.Venue,
        Status = dto.Status,
        ExistingDistancesKm = dto.DistancesKm,
    };

    private void ApplyApiErrors<T>(ApiResult<T> result)
    {
        if (result.ValidationErrors is not null)
        {
            foreach (var (field, errors) in result.ValidationErrors)
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError(field, error);
                }
            }
        }
        else
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Something went wrong. Please try again.");
        }
    }
}
