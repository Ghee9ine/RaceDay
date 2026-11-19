using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services.Api;

namespace RaceDay.Web.Infrastructure;

/// <summary>Registered globally (Program.cs). If the API ever returns 401 while this app's own
/// session still thinks the user is logged in - the API process restarted and its in-memory
/// session store (RaceDay.Api's AddDistributedMemoryCache) lost everything - clear this session
/// too and send the user back to login with an explanation, rather than letting the exception
/// surface as a 500.</summary>
public class ApiUnauthorizedExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ApiUnauthorizedException)
        {
            return;
        }

        context.HttpContext.Session.Clear();

        // "expired=true" rather than TempData: TempData wouldn't survive
        // being set mid-pipeline from inside an exception filter the way
        // it does from a normal controller action, and the query string is
        // simple and sufficient here - AccountController.Login reads it
        // and shows "Your session expired, please log in again."
        var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
        context.Result = new RedirectToActionResult("Login", "Account", new { area = "", returnUrl, expired = true });
        context.ExceptionHandled = true;
    }
}
