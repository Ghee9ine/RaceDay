using RaceDay.Api.Dtos.Users;

namespace RaceDay.Api.Services;

public interface IUserService
{
    Task<UserProfileResponse?> GetProfileAsync(int userId);
    Task<UserProfileResponse?> UpdateProfileAsync(int userId, UpdateProfileRequest request);
}
