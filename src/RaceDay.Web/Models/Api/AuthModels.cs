namespace RaceDay.Web.Models.Api;

// Plain records mirroring RaceDay.Api's own Dtos - duplicated deliberately,
// not shared via a project reference. See RaceDay.Web.csproj's own comment
// and the README's "MVC architecture" section for why.

public record LoginRequestDto(string Email, string Password);
public record LoginResponseDto(int UserId, string FullName, string Email, string Role);
public record RegisterRequestDto(string FullName, string Email, string Password, string Role, string? PhoneNumber);
public record RegisterResponseDto(int UserId, string Role);

public record UserProfileDto(int UserId, string FullName, string Email, string Role, string? PhoneNumber, string? ProfilePictureUrl, DateTime CreatedAt);
public record UpdateProfileRequestDto(string FullName, string? PhoneNumber);
public record ProfilePictureUploadedDto(string ProfilePictureUrl);
