using RaceDay.Web.Infrastructure;

namespace RaceDay.Web.Services.Api;

/// <summary>
/// Attaches the CURRENT user's own API session cookie to every outgoing
/// request. Registered transient and added to RaceDayApiClient's
/// HttpClient pipeline, whose primary handler is configured with
/// UseCookies = false (see Program.cs) - pooled HttpClientHandlers are
/// shared across every user of this app, so a shared CookieContainer would
/// leak one user's .RaceDay.Session value to a completely different
/// user's requests. Reading the cookie value fresh from THIS request's own
/// MVC session on every call, instead, keeps each user's forwarded
/// identity correctly scoped to themselves.
/// </summary>
public class ApiSessionCookieHandler : DelegatingHandler
{
    private const string ApiSessionCookieName = ".RaceDay.Session";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiSessionCookieHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cookieValue = _httpContextAccessor.HttpContext?.Session.GetString(SessionKeys.ApiSessionCookie);
        if (!string.IsNullOrEmpty(cookieValue))
        {
            request.Headers.Add("Cookie", $"{ApiSessionCookieName}={cookieValue}");
        }

        return base.SendAsync(request, cancellationToken);
    }
}
