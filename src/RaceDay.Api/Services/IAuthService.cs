using RaceDay.Api.Dtos.Auth;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public enum RegisterOutcome
{
    Success,
    EmailAlreadyRegistered,
}

public record RegisterResult(RegisterOutcome Outcome, User? User);

public interface IAuthService
{
    Task<RegisterResult> RegisterAsync(RegisterRequest request);

    /// <summary>Returns the matching user if the email/password pair is valid, otherwise null.
    /// Never reveals which half (email or password) was wrong.</summary>
    Task<User?> VerifyCredentialsAsync(string email, string password);
}
