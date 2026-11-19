using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Users;
using RaceDay.Api.Services.Storage;

namespace RaceDay.Api.Services;

public class UserService : IUserService
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobStorage;

    public UserService(RaceDayDbContext db, IBlobStorageService blobStorage)
    {
        _db = db;
        _blobStorage = blobStorage;
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

    public async Task<ServiceResult<string>> SetProfilePictureAsync(int userId, IFormFile file, CancellationToken ct)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.UserId == userId, ct);
        if (user is null)
        {
            return ServiceResult<string>.NotFound();
        }

        var validation = await ImageValidator.ValidateAsync(file);
        if (!validation.IsValid)
        {
            return ServiceResult<string>.ValidationFailed(ImageValidationMessages.For(validation.Outcome));
        }

        var previousUrl = user.ProfilePictureUrl;
        var blobPath = $"users/{userId}/{Guid.NewGuid():N}.{validation.Extension}";

        await using var stream = file.OpenReadStream();
        var url = await _blobStorage.UploadImageAsync(stream, file.ContentType, blobPath, ct);

        user.ProfilePictureUrl = url;
        await _db.SaveChangesAsync(ct);

        await _blobStorage.DeleteIfExistsAsync(previousUrl, ct);

        return ServiceResult<string>.Success(url);
    }
}
