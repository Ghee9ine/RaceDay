using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Dtos.Results;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Capturing, correcting and viewing race results.</summary>
[ApiController]
[Tags("Results")]
public class ResultsController : ControllerBase
{
    private readonly IResultService _resultService;

    public ResultsController(IResultService resultService)
    {
        _resultService = resultService;
    }

    /// <summary>Captures a finish result for a participant's enrolment. Owner of the event only.</summary>
    /// <response code="201">The captured result.</response>
    /// <response code="400">Validation failed (Finished requires finishTime and position).</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">No such enrolment.</response>
    /// <response code="409">A result already exists for this enrolment, or it's cancelled.</response>
    [HttpPost("api/enrolments/{enrolmentId:int}/results")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(int enrolmentId, CaptureResultRequest request)
    {
        var result = await _resultService.CaptureAsync(enrolmentId, User.GetUserId(), request);
        return result.Outcome switch
        {
            ServiceOutcome.NotFound => NotFound(),
            ServiceOutcome.Forbidden => Forbid(),
            ServiceOutcome.Conflict => Conflict(new { message = "A result already exists for this enrolment, or it has been cancelled." }),
            _ => StatusCode(StatusCodes.Status201Created, result.Value),
        };
    }

    /// <summary>Corrects a previously captured result. Owner of the event only.</summary>
    /// <response code="200">The updated result.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">No such result.</response>
    [HttpPut("api/results/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CaptureResultRequest request)
    {
        var result = await _resultService.UpdateAsync(id, User.GetUserId(), request);
        return result.Outcome switch
        {
            ServiceOutcome.NotFound => NotFound(),
            ServiceOutcome.Forbidden => Forbid(),
            _ => Ok(result.Value),
        };
    }

    /// <summary>Returns the authenticated participant's full personal result history.</summary>
    /// <response code="200">The caller's results.</response>
    [HttpGet("api/results/me")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(List<MyResultResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine()
    {
        var results = await _resultService.GetMineAsync(User.GetUserId());
        return Ok(results);
    }

    /// <summary>Returns the public leaderboard for a category: Finished results ordered by finish time, then DNF/DSQ.</summary>
    /// <response code="200">The leaderboard.</response>
    /// <response code="404">No such category.</response>
    [HttpGet("api/categories/{categoryId:int}/results")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<LeaderboardEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLeaderboard(int categoryId)
    {
        var leaderboard = await _resultService.GetLeaderboardAsync(categoryId);
        return leaderboard is null ? NotFound() : Ok(leaderboard);
    }
}
