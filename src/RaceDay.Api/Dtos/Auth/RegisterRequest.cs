using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Dtos.Validation;

namespace RaceDay.Api.Dtos.Auth;

// Validation attributes go directly on the primary constructor parameters,
// not via a [property: ...] target: ASP.NET Core's model validation reads
// a record's metadata from its constructor parameters, and throws at
// runtime if it instead finds the attributes attached to the generated
// property.
public record RegisterRequest(
    [Required, MaxLength(150)] string FullName,
    [Required, EmailAddress, MaxLength(200)] string Email,
    [Required, StrongPassword] string Password,
    [Required, ValidRole] string Role,
    [MaxLength(20)] string? PhoneNumber
);
