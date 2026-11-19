using Microsoft.AspNetCore.Http;
using RaceDay.Api.Dtos.Users;

namespace RaceDay.Api.Services;

public interface IUserService
{
    Task<UserProfileResponse?> GetProfileAsync(int userId);
    Task<UserProfileResponse?> UpdateProfileAsync(int userId, UpdateProfileRequest request);

    /// <summary>Uploads and sets the authenticated user's own profile picture, replacing (and
    /// deleting) any previous one only after the new URL has been saved successfully.</summary>
    Task<ServiceResult<string>> SetProfilePictureAsync(int userId, IFormFile file, CancellationToken ct);
}
