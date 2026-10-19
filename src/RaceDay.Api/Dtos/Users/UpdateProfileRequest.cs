using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos.Users;

public record UpdateProfileRequest(
    [Required, MaxLength(150)] string FullName,
    [MaxLength(20)] string? PhoneNumber
);
