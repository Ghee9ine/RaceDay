namespace RaceDay.Api.Dtos.Auth;

public record LoginResponse(int UserId, string FullName, string Email, string Role);
