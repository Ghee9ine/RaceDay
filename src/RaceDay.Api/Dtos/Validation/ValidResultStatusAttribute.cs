using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models.Enums;

namespace RaceDay.Api.Dtos.Validation;

public class ValidResultStatusAttribute : ValidationAttribute
{
    public ValidResultStatusAttribute()
    {
        ErrorMessage = "Status must be 'Finished', 'DNF' or 'DSQ'.";
    }

    public override bool IsValid(object? value) => value is string v && ResultStatuses.IsValid(v);
}
