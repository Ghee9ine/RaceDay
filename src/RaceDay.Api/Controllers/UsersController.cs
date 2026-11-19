using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Dtos.Uploads;
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

    /// <summary>Uploads the authenticated user's profile picture to Azure Blob Storage and stores its URL.</summary>
    /// <response code="200">The profile picture's new URL.</response>
    /// <response code="400">No file, the file is over 5 MB, or its type/content isn't a supported image.</response>
    /// <response code="401">No active session.</response>
    /// <response code="503">Blob storage is not configured on this deployment.</response>
    [HttpPost("me/profile-picture")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_242_880)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadProfilePicture([FromForm] ImageUploadRequest request, CancellationToken ct)
    {
        var result = await _userService.SetProfilePictureAsync(User.GetUserId(), request.File, ct);
        return result.Outcome switch
        {
            ServiceOutcome.ValidationFailed => BadRequest(new { message = result.Error }),
            _ => Ok(new { profilePictureUrl = result.Value }),
        };
    }
}
