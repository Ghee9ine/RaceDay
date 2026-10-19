using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Dtos.Enrolments;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Participants entering categories, and organisers viewing who's entered.</summary>
[ApiController]
[Tags("Event Enrolments")]
public class EnrolmentsController : ControllerBase
{
    private readonly IEnrolmentService _enrolmentService;

    public EnrolmentsController(IEnrolmentService enrolmentService)
    {
        _enrolmentService = enrolmentService;
    }

    /// <summary>Enters the authenticated participant into a category and assigns a bib number.
    /// Reactivates a previously cancelled enrolment in the same category instead of duplicating it.</summary>
    /// <response code="201">The new (or reactivated) enrolment.</response>
    /// <response code="404">No such category.</response>
    /// <response code="409">The event isn't published or is already past, the participant is already
    /// confirmed, or the category is full.</response>
    [HttpPost("api/categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        var result = await _enrolmentService.EnrolAsync(User.GetUserId(), categoryId);
        return result.Outcome switch
        {
            ServiceOutcome.NotFound => NotFound(),
            ServiceOutcome.Conflict => Conflict(new { message = "Unable to enrol: the event isn't open for entries, you're already entered, or the category is full." }),
            _ => StatusCode(StatusCodes.Status201Created, result.Value),
        };
    }

    /// <summary>Returns all of the authenticated participant's own enrolments, newest first.</summary>
    /// <response code="200">The caller's enrolments.</response>
    [HttpGet("api/enrolments/me")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine()
    {
        var enrolments = await _enrolmentService.GetMineAsync(User.GetUserId());
        return Ok(enrolments);
    }

    /// <summary>Lists every enrolment across all categories of an event, for race-day logistics. Owner only.</summary>
    /// <response code="200">The event's enrolments.</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">No such event.</response>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(List<OrganiserEnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEvent(int eventId)
    {
        var result = await _enrolmentService.GetByEventAsync(eventId, User.GetUserId());
        return result.Outcome switch
        {
            ServiceOutcome.NotFound => NotFound(),
            ServiceOutcome.Forbidden => Forbid(),
            _ => Ok(result.Value),
        };
    }

    /// <summary>Cancels (soft delete) the authenticated participant's own enrolment.</summary>
    /// <response code="204">Cancelled.</response>
    /// <response code="403">The caller does not own this enrolment.</response>
    /// <response code="404">No such enrolment.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpDelete("api/enrolments/{id:int}")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _enrolmentService.CancelAsync(id, User.GetUserId());
        return result.Outcome switch
        {
            ServiceOutcome.NotFound => NotFound(),
            ServiceOutcome.Forbidden => Forbid(),
            ServiceOutcome.Conflict => Conflict(new { message = "A result has already been captured for this enrolment." }),
            _ => NoContent(),
        };
    }
}
