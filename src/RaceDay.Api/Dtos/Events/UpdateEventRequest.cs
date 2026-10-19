using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Dtos.Validation;

namespace RaceDay.Api.Dtos.Events;

public record UpdateEventRequest(
    [Required, MaxLength(150)] string Name,
    string? Description,
    [Required, ValidDisciplineType] string DisciplineType,
    [Required] DateOnly EventDate,
    TimeOnly? StartTime,
    [MaxLength(50)] string? Province,
    [MaxLength(200)] string? Venue,
    [Required, ValidEventStatus] string Status
);
