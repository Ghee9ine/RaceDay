using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services.Api;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

[RequireLogin]
public class ProfileController : Controller
{
    private readonly IRaceDayApiClient _api;

    public ProfileController(IRaceDayApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyProfileAsync();
        if (!result.Success)
        {
            TempData["ToastError"] = "Couldn't load your profile.";
            return RedirectToAction("Index", "Home");
        }

        return View(ToViewModel(result.Data!));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _api.UpdateMyProfileAsync(new UpdateProfileRequestDto(model.FullName, model.PhoneNumber));
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Couldn't update your profile.");
            return View(model);
        }

        HttpContext.Session.SetString(SessionKeys.FullName, result.Data!.FullName);
        TempData["ToastSuccess"] = "Profile updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["ToastError"] = "Please choose an image to upload.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UploadProfilePictureAsync(file);
        if (!result.Success)
        {
            TempData["ToastError"] = result.ErrorMessage ?? "Couldn't upload that image.";
            return RedirectToAction(nameof(Index));
        }

        HttpContext.Session.SetString(SessionKeys.ProfilePictureUrl, result.Data!.ProfilePictureUrl);
        TempData["ToastSuccess"] = "Profile picture updated.";
        return RedirectToAction(nameof(Index));
    }

    private static ProfileViewModel ToViewModel(UserProfileDto dto) => new()
    {
        UserId = dto.UserId,
        Email = dto.Email,
        Role = dto.Role,
        ProfilePictureUrl = dto.ProfilePictureUrl,
        FullName = dto.FullName,
        PhoneNumber = dto.PhoneNumber,
    };
}
