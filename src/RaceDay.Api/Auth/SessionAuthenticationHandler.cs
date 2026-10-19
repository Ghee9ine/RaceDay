using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Auth;

/// <summary>
/// Turns the server-side session into a ClaimsPrincipal: [Authorize] reads
/// claims, not the session directly, so this is the one place that bridges
/// the two. Login (AuthController) writes UserId/Role into
/// HttpContext.Session; this handler reads them back out on every later
/// request on the same session. No cookie-based redirect is configured, so
/// a missing/invalid session challenges with a plain 401, and a wrong role
/// forbids with a plain 403 - never a redirect to a login page, since this
/// is an API, not a browser app.
/// </summary>
public class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Session";

    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Context.Session.GetInt32(SessionKeys.UserId);
        var role = Context.Session.GetString(SessionKeys.Role);

        if (userId is null || string.IsNullOrEmpty(role))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
            new Claim(ClaimTypes.Role, role),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
