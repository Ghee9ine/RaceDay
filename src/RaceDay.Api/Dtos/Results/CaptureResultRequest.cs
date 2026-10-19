using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Dtos.Validation;
using RaceDay.Api.Models.Enums;

namespace RaceDay.Api.Dtos.Results;

// Shared shape for both capture and correction. Implements IValidatableObject
// because "Finished requires finishTime and position, DNF/DSQ allow both to
// be null" is a cross-field rule DataAnnotations attributes alone can't
// express on their own.
public record CaptureResultRequest(
    TimeOnly? FinishTime,
    int? Position,
    [Required, ValidResultStatus] string Status
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status == ResultStatuses.Finished && (FinishTime is null || Position is null))
        {
            yield return new ValidationResult(
                "finishTime and position are both required when status is 'Finished'.",
                new[] { nameof(FinishTime), nameof(Position) });
        }
    }
}
