using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace RaceDay.Api.Dtos.Validation;

/// <summary>Requires at least 8 characters, with at least one letter and one digit.</summary>
public partial class StrongPasswordAttribute : ValidationAttribute
{
    public StrongPasswordAttribute()
    {
        ErrorMessage = "Password must be at least 8 characters long and contain at least one letter and one digit.";
    }

    public override bool IsValid(object? value)
    {
        if (value is not string password)
        {
            return false;
        }

        return password.Length >= 8 && PasswordPattern().IsMatch(password);
    }

    [GeneratedRegex(@"(?=.*[A-Za-z])(?=.*\d)")]
    private static partial Regex PasswordPattern();
}
