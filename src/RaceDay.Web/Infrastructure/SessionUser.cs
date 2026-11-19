namespace RaceDay.Web.Infrastructure;

/// <summary>Thin, read-only view over the current request's MVC session, so views and
/// controllers read "am I logged in / what role am I" through one place instead of poking
/// HttpContext.Session directly everywhere.</summary>
public class SessionUser
{
    private readonly ISession _session;

    public SessionUser(IHttpContextAccessor httpContextAccessor)
    {
        _session = httpContextAccessor.HttpContext!.Session;
    }

    public bool IsAuthenticated => _session.GetInt32(SessionKeys.UserId) is not null;
    public int? UserId => _session.GetInt32(SessionKeys.UserId);
    public string? Role => _session.GetString(SessionKeys.Role);
    public string? FullName => _session.GetString(SessionKeys.FullName);
    public string? Email => _session.GetString(SessionKeys.Email);
    public string? ProfilePictureUrl => _session.GetString(SessionKeys.ProfilePictureUrl);

    public bool IsOrganiser => Role == "Organiser";
    public bool IsParticipant => Role == "Participant";

    /// <summary>Initials for the default avatar when no profile picture is set, e.g. "Thandiwe
    /// Mokoena" -> "TM".</summary>
    public string Initials
    {
        get
        {
            var name = FullName ?? string.Empty;
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..1].ToUpperInvariant(),
                _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant(),
            };
        }
    }
}
