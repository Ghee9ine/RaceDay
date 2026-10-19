using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models.Enums;

namespace RaceDay.Api.Dtos.Validation;

/// <summary>Accepts only "Organiser" or "Participant" - no other role, including "Admin".</summary>
public class ValidRoleAttribute : ValidationAttribute
{
    public ValidRoleAttribute()
    {
        ErrorMessage = "Role must be either 'Organiser' or 'Participant'.";
    }

    public override bool IsValid(object? value) => value is string role && Roles.IsValid(role);
}
