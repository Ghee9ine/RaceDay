using System.Security.Claims;

namespace RaceDay.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The authenticated caller's UserId, from the claim SessionAuthenticationHandler sets.
    /// Throws if called on an unauthenticated principal - every caller of this should already be
    /// behind [Authorize].</summary>
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(value ?? throw new InvalidOperationException("No authenticated user id on this principal."));
    }
}
