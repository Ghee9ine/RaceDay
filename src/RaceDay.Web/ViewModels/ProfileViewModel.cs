using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.ViewModels;

public class ProfileViewModel
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }

    [Required, MaxLength(150), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Phone, MaxLength(20), Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }
}
