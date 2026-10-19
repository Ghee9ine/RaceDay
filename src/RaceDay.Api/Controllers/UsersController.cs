using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Dtos.Users;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>The authenticated caller's own profile. Either role.</summary>
[ApiController]
[Route("api/users")]
[Tags("User Profile")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Returns the profile of the currently authenticated user.</summary>
    /// <response code="200">The caller's profile.</response>
    /// <response code="401">No active session.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var profile = await _userService.GetProfileAsync(User.GetUserId());
        return Ok(profile);
    }

    /// <summary>Updates the authenticated user's own profile details.</summary>
    /// <response code="200">Updated profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">No active session.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        var profile = await _userService.UpdateProfileAsync(User.GetUserId(), request);
        return Ok(profile);
    }
}
