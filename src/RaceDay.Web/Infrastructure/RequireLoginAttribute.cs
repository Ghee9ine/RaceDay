using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Infrastructure;

/// <summary>Redirects to the login page, with a returnUrl back to the page that was actually
/// requested, if nothing in the MVC session says the caller is logged in. Session-based, not
/// ASP.NET Core's [Authorize] - this app has no cookie-auth middleware of its own, it reads the
/// same session SessionUser does.</summary>
public class RequireLoginAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        if (session.GetInt32(SessionKeys.UserId) is null)
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "", returnUrl });
        }
    }
}
