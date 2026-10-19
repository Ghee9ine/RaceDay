namespace RaceDay.Api.Services;

/// <summary>
/// Shared outcome shape for services whose actions a controller needs to
/// translate into 403/404/409 as well as success, instead of just
/// null-means-not-found. Reused across Events, Categories, Enrolments and
/// Results so every controller maps outcomes to status codes the same way.
/// </summary>
public enum ServiceOutcome
{
    Success,
    NotFound,
    Forbidden,
    Conflict,
}

public record ServiceResult<T>(ServiceOutcome Outcome, T? Value = default)
{
    public static ServiceResult<T> Success(T value) => new(ServiceOutcome.Success, value);
    public static ServiceResult<T> NotFound() => new(ServiceOutcome.NotFound);
    public static ServiceResult<T> Forbidden() => new(ServiceOutcome.Forbidden);
    public static ServiceResult<T> Conflict() => new(ServiceOutcome.Conflict);
}
