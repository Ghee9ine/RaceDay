using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Dtos.Events;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Browsing, organiser management and ownership-checked CRUD for events.</summary>
[ApiController]
[Route("api/events")]
[Tags("Events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    /// <summary>Lists all published events, with optional filters, ordered by date.</summary>
    /// <response code="200">The matching published events.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublished(
        [FromQuery] string? province,
        [FromQuery] string? disciplineType,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var events = await _eventService.GetPublishedAsync(new EventFilter(province, disciplineType, from, to));
        return Ok(events);
    }

    /// <summary>Lists every event owned by the authenticated organiser, in any status, with enrolment counts.</summary>
    /// <response code="200">The caller's events.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">The caller is not an Organiser.</response>
    [HttpGet("mine")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(List<MyEventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var events = await _eventService.GetMineAsync(User.GetUserId());
        return Ok(events);
    }

    /// <summary>Returns full details for a single event. A Draft event 404s to anyone but its owner.</summary>
    /// <response code="200">The event.</response>
    /// <response code="404">No such event (or it's a Draft and the caller isn't its owner).</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        int? callerUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        var detail = await _eventService.GetByIdAsync(id, callerUserId);
        return detail is null ? NotFound() : Ok(detail);
    }

    /// <summary>Creates a new event owned by the authenticated organiser.</summary>
    /// <response code="201">The new event.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateEventRequest request)
    {
        var created = await _eventService.CreateAsync(User.GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { id = created.EventId }, created);
    }

    /// <summary>Updates an existing event's details. Owner only.</summary>
    /// <response code="200">The updated event.</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">No such event.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateEventRequest request)
    {
        var result = await _eventService.UpdateAsync(id, User.GetUserId(), request);
        return result.Outcome switch
        {
            ServiceOutcome.NotFound => NotFound(),
            ServiceOutcome.Forbidden => Forbid(),
            _ => Ok(result.Value),
        };
    }

    /// <summary>Deletes an event and its categories/routes, provided no enrolments exist. Owner only.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">No such event.</response>
    /// <response code="409">The event already has enrolments.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _eventService.DeleteAsync(id, User.GetUserId());
        return result.Outcome switch
        {
            ServiceOutcome.NotFound => NotFound(),
            ServiceOutcome.Forbidden => Forbid(),
            ServiceOutcome.Conflict => Conflict(new { message = "This event already has enrolments and can't be deleted." }),
            _ => NoContent(),
        };
    }
}
