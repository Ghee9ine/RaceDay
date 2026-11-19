using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress, Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
    public bool Expired { get; set; }
}

public class RegisterViewModel
{
    [Required, MaxLength(150), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, MaxLength(20), Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Required, DataType(DataType.Password), MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Participant";
}
