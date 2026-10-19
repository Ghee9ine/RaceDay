using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos.Auth;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);
