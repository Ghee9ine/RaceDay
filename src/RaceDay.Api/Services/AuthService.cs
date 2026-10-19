using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Auth;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public class AuthService : IAuthService
{
    private readonly RaceDayDbContext _db;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthService(RaceDayDbContext db)
    {
        _db = db;
    }

    public async Task<RegisterResult> RegisterAsync(RegisterRequest request)
    {
        var emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
        {
            return new RegisterResult(RegisterOutcome.EmailAlreadyRegistered, null);
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Role = request.Role,
            PhoneNumber = request.PhoneNumber,
            // Set explicitly rather than relying on the column's
            // SYSUTCDATETIME() default: that default is SQL Server syntax
            // and the test suite runs against SQLite, which doesn't have it.
            CreatedAt = DateTime.UtcNow,
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new RegisterResult(RegisterOutcome.Success, user);
    }

    public async Task<User?> VerifyCredentialsAsync(string email, string password)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            // Still worth a hashing pass so this path takes a similar amount
            // of time whether or not the email exists, instead of returning
            // early and leaking that distinction through response timing.
            _hasher.HashPassword(new User(), password);
            return null;
        }

        var verification = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return verification is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded
            ? user
            : null;
    }
}
