namespace RaceDay.Web.Services.Api;

/// <summary>Thrown when the API returns 401 while the MVC session believes the user is still
/// logged in - e.g. the API process restarted and its in-memory session store (see RaceDay.Api's
/// AddDistributedMemoryCache) lost every session. ApiUnauthorizedExceptionFilter catches this,
/// clears the MVC session, and redirects to login with a "session expired" message.</summary>
public class ApiUnauthorizedException : Exception
{
    public ApiUnauthorizedException() : base("The API session has expired or is invalid.")
    {
    }
}
