namespace RaceDay.Web.Infrastructure;

/// <summary>Key names for everything this app stores in the MVC session (.RaceDay.Web.Session) -
/// not to be confused with the API's own, separate session (.RaceDay.Session), whose cookie
/// value is itself one of the things stored here under ApiSessionCookie.</summary>
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FullName = "FullName";
    public const string Email = "Email";
    public const string ProfilePictureUrl = "ProfilePictureUrl";

    /// <summary>The raw value of the API's .RaceDay.Session cookie for this specific user,
    /// forwarded on every API call by ApiSessionCookieHandler.</summary>
    public const string ApiSessionCookie = "ApiSessionCookie";
}
