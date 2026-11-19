using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services.Api;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly IRaceDayApiClient _api;

    public AccountController(IRaceDayApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl, bool expired = false)
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId) is not null)
        {
            return RedirectToLandingPage();
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl, Expired = expired });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _api.LoginAsync(model.Email, model.Password, HttpContext);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Invalid email or password.");
            return View(model);
        }

        var user = result.Data!;
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);
        HttpContext.Session.SetString(SessionKeys.FullName, user.FullName);
        HttpContext.Session.SetString(SessionKeys.Email, user.Email);

        var profile = await _api.GetMyProfileAsync();
        if (profile.Success && !string.IsNullOrEmpty(profile.Data!.ProfilePictureUrl))
        {
            HttpContext.Session.SetString(SessionKeys.ProfilePictureUrl, profile.Data.ProfilePictureUrl);
        }

        TempData["ToastSuccess"] = $"Welcome back, {user.FullName}.";

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToLandingPage(user.Role);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId) is not null)
        {
            return RedirectToLandingPage();
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = new RegisterRequestDto(model.FullName, model.Email, model.Password, model.Role, model.PhoneNumber);
        var result = await _api.RegisterAsync(request);

        if (!result.Success)
        {
            ApplyApiErrors(result);
            return View(model);
        }

        TempData["ToastSuccess"] = "Account created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.LogoutAsync();
        HttpContext.Session.Clear();
        TempData["ToastSuccess"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLandingPage(string? role = null)
    {
        role ??= HttpContext.Session.GetString(SessionKeys.Role);
        return role switch
        {
            "Organiser" => RedirectToAction("Index", "Dashboard"),
            _ => RedirectToAction("Index", "Home"),
        };
    }

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
