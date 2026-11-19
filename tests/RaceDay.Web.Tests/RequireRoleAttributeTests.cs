using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Tests.Infrastructure;

namespace RaceDay.Web.Tests;

public class RequireRoleAttributeTests
{
    private static ActionExecutingContext BuildContext(ISession session)
    {
        var httpContext = new DefaultHttpContext { Session = session };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: new object());
    }

    [Fact]
    public void NoSession_RedirectsToLoginWithReturnUrl()
    {
        var session = new FakeSession();
        var context = BuildContext(session);
        context.HttpContext.Request.Path = "/Dashboard";

        new RequireRoleAttribute("Organiser").OnActionExecuting(context);

        var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("Account", redirect.ControllerName);
        Assert.Equal("/Dashboard", redirect.RouteValues!["returnUrl"]);
    }

    [Fact]
    public void WrongRole_RedirectsToAccessDenied()
    {
        var session = new FakeSession();
        session.SetInt32(SessionKeys.UserId, 1);
        session.SetString(SessionKeys.Role, "Participant");
        var context = BuildContext(session);

        new RequireRoleAttribute("Organiser").OnActionExecuting(context);

        var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
        Assert.Equal("AccessDenied", redirect.ActionName);
        Assert.Equal("Account", redirect.ControllerName);
    }

    [Fact]
    public void MatchingRole_AllowsRequestThrough()
    {
        var session = new FakeSession();
        session.SetInt32(SessionKeys.UserId, 1);
        session.SetString(SessionKeys.Role, "Organiser");
        var context = BuildContext(session);

        new RequireRoleAttribute("Organiser").OnActionExecuting(context);

        Assert.Null(context.Result);
    }
}
