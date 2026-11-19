using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Infrastructure;

/// <summary>No session at all -> redirect to login (with returnUrl), same as RequireLoginAttribute.
/// Logged in but the wrong role -> redirect to AccessDenied, never silently allowed through.
/// Apply at controller level for every Organiser-only / Participant-only controller.</summary>
public class RequireRoleAttribute : ActionFilterAttribute
{
    private readonly string _role;

    public RequireRoleAttribute(string role)
    {
        _role = role;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);

        if (userId is null)
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "", returnUrl });
            return;
        }

        var role = session.GetString(SessionKeys.Role);
        if (role != _role)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", new { area = "" });
        }
    }
}
