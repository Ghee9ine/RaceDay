using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Users;

namespace RaceDay.Api.Services;

public class UserService : IUserService
{
    private readonly RaceDayDbContext _db;

    public UserService(RaceDayDbContext db)
    {
        _db = db;
    }

    public async Task<UserProfileResponse?> GetProfileAsync(int userId)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new UserProfileResponse(u.UserId, u.FullName, u.Email, u.Role, u.PhoneNumber, u.ProfilePictureUrl, u.CreatedAt))
            .SingleOrDefaultAsync();
    }

    public async Task<UserProfileResponse?> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.UserId == userId);
        if (user is null)
        {
            return null;
        }

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        await _db.SaveChangesAsync();

        return new UserProfileResponse(user.UserId, user.FullName, user.Email, user.Role, user.PhoneNumber, user.ProfilePictureUrl, user.CreatedAt);
    }
}
