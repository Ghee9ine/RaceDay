using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models.Enums;

namespace RaceDay.Api.Dtos.Validation;

/// <summary>Allows null (the request DTO decides whether a missing status defaults to Published).</summary>
public class ValidEventStatusAttribute : ValidationAttribute
{
    public ValidEventStatusAttribute()
    {
        ErrorMessage = "Status must be 'Draft', 'Published', 'Cancelled' or 'Completed'.";
    }

    public override bool IsValid(object? value) => value is null || (value is string v && EventStatuses.IsValid(v));
}
