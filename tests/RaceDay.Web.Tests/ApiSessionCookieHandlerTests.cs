using Microsoft.AspNetCore.Http;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services.Api;
using RaceDay.Web.Tests.Infrastructure;

namespace RaceDay.Web.Tests;

public class ApiSessionCookieHandlerTests
{
    private static (HttpClient client, CapturingHandler captured) BuildClient(ISession session)
    {
        var httpContext = new DefaultHttpContext { Session = session };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        var captured = new CapturingHandler();
        var cookieHandler = new ApiSessionCookieHandler(accessor) { InnerHandler = captured };
        var client = new HttpClient(cookieHandler) { BaseAddress = new Uri("https://localhost/") };
        return (client, captured);
    }

    [Fact]
    public async Task WithSessionCookie_AttachesItToTheOutgoingRequest()
    {
        var session = new FakeSession();
        session.SetString(SessionKeys.ApiSessionCookie, "abc123");
        var (client, captured) = BuildClient(session);

        await client.GetAsync("api/events");

        Assert.True(captured.LastRequest!.Headers.TryGetValues("Cookie", out var cookies));
        Assert.Contains(".RaceDay.Session=abc123", cookies!.Single());
    }

    [Fact]
    public async Task WithoutSessionCookie_SendsNoCookieHeader()
    {
        var session = new FakeSession();
        var (client, captured) = BuildClient(session);

        await client.GetAsync("api/events");

        Assert.False(captured.LastRequest!.Headers.Contains("Cookie"));
    }

    [Fact]
    public async Task DifferentSessions_NeverLeakEachOthersCookie()
    {
        var sessionA = new FakeSession();
        sessionA.SetString(SessionKeys.ApiSessionCookie, "user-a-cookie");
        var (clientA, capturedA) = BuildClient(sessionA);
        await clientA.GetAsync("api/events");

        var sessionB = new FakeSession();
        sessionB.SetString(SessionKeys.ApiSessionCookie, "user-b-cookie");
        var (clientB, capturedB) = BuildClient(sessionB);
        await clientB.GetAsync("api/events");

        Assert.Contains("user-a-cookie", capturedA.LastRequest!.Headers.GetValues("Cookie").Single());
        Assert.Contains("user-b-cookie", capturedB.LastRequest!.Headers.GetValues("Cookie").Single());
    }
}
