namespace RaceDay.Api.Dtos.Users;

public record UserProfileResponse(
    int UserId,
    string FullName,
    string Email,
    string Role,
    string? PhoneNumber,
    string? ProfilePictureUrl,
    DateTime CreatedAt
);
