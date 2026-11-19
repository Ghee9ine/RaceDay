using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Services.Storage;

namespace RaceDay.Api.Middleware;

/// <summary>
/// Catches anything a controller/service didn't handle itself and turns it
/// into a ProblemDetails response (RFC 7807) instead of letting ASP.NET
/// Core's default behaviour leak a stack trace to the caller. Registered
/// with AddProblemDetails()/AddExceptionHandler&lt;T&gt;() and
/// UseExceptionHandler() in Program.cs. One specific exception type,
/// BlobStorageNotConfiguredException, maps to 503 rather than the generic
/// 500 below, since it's an expected, actionable configuration state (no
/// connection string set), not an actual bug.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        if (exception is BlobStorageNotConfiguredException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Blob storage is not configured on this deployment.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
            }, cancellationToken);
            return true;
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
