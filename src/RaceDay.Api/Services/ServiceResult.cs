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

    /// <summary>An application-level 400, e.g. a duplicate category name for
    /// the same event - a rule the plan calls out as 400, not 409, since
    /// it's caught before anything is persisted, not a state clash.</summary>
    ValidationFailed,
}

public record ServiceResult<T>(ServiceOutcome Outcome, T? Value = default, string? Error = null)
{
    public static ServiceResult<T> Success(T value) => new(ServiceOutcome.Success, value);
    public static ServiceResult<T> NotFound() => new(ServiceOutcome.NotFound);
    public static ServiceResult<T> Forbidden() => new(ServiceOutcome.Forbidden);
    public static ServiceResult<T> Conflict() => new(ServiceOutcome.Conflict);
    public static ServiceResult<T> ValidationFailed(string error) => new(ServiceOutcome.ValidationFailed, default, error);
}
