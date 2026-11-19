using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Web.Controllers;

/// <summary>Targets of UseStatusCodePagesWithReExecute("/error/{0}") and
/// UseExceptionHandler("/error/500") (Program.cs) - friendly pages instead of the ASP.NET Core
/// default error screens or a raw stack trace.</summary>
public class ErrorController : Controller
{
    [Route("/error/{code:int}")]
    public IActionResult Index(int code)
    {
        Response.StatusCode = code;
        ViewData["Title"] = code switch
        {
            404 => "Page not found",
            403 => "Access denied",
            _ => "Something went wrong",
        };
        return View("Error", code);
    }
}
