namespace RaceDay.Api.Auth;

/// <summary>The two values login writes into HttpContext.Session and that
/// SessionAuthenticationHandler reads back out on every later request.</summary>
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
}
