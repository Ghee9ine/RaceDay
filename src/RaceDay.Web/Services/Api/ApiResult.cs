using System.Net;

namespace RaceDay.Web.Services.Api;

/// <summary>
/// What every RaceDayApiClient method returns instead of throwing on an
/// expected failure (400/404/409 etc.) - a controller inspects this and
/// decides what to show, rather than catching exceptions for routine,
/// expected outcomes. ApiUnauthorizedException (401) is the one exception
/// that's still thrown, because it means something more structural (the
/// MVC session thinks the user is logged in but the API disagrees) than a
/// per-request validation/conflict result.
/// </summary>
public record ApiResult<T>(bool Success, T? Data, HttpStatusCode StatusCode, string? ErrorMessage = null, IDictionary<string, string[]>? ValidationErrors = null)
{
    public static ApiResult<T> Ok(T data, HttpStatusCode statusCode = HttpStatusCode.OK) => new(true, data, statusCode);

    public static ApiResult<T> Fail(HttpStatusCode statusCode, string? errorMessage = null, IDictionary<string, string[]>? validationErrors = null)
        => new(false, default, statusCode, errorMessage, validationErrors);
}
