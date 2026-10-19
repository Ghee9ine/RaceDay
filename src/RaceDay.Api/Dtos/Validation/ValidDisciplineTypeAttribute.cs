using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models.Enums;

namespace RaceDay.Api.Dtos.Validation;

public class ValidDisciplineTypeAttribute : ValidationAttribute
{
    public ValidDisciplineTypeAttribute()
    {
        ErrorMessage = "DisciplineType must be 'Running', 'Cycling' or 'Walking'.";
    }

    public override bool IsValid(object? value) => value is string v && DisciplineTypes.IsValid(v);
}
