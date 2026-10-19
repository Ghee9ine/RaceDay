using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Dtos.Auth;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Registration and session-based login/logout.</summary>
[ApiController]
[Route("api/auth")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Creates a new account as either an Organiser or a Participant. Does not log the new user in.</summary>
    /// <response code="201">Account created.</response>
    /// <response code="400">Validation failed (including an invalid role or a weak password).</response>
    /// <response code="409">That email is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        if (result.Outcome == RegisterOutcome.EmailAlreadyRegistered)
        {
            return Conflict(new { message = "That email address is already registered." });
        }

        var user = result.User!;
        // /api/users/me is a fixed, parameterless route (it reads the
        // caller from the session, not from an id in the URL), so there's
        // no id-based action to link to with CreatedAtAction - the header
        // is set directly instead.
        Response.Headers.Location = "/api/users/me";
        return StatusCode(StatusCodes.Status201Created, new RegisterResponse(user.UserId, user.Role));
    }

    /// <summary>Authenticates a user and starts a server-side session. The session store
    /// records the user's id and role; the response sets the .RaceDay.Session cookie, which
    /// the client must send on every later request.</summary>
    /// <response code="200">Authenticated; session cookie set.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _authService.VerifyCredentialsAsync(request.Email, request.Password);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);
        await HttpContext.Session.CommitAsync();

        return Ok(new LoginResponse(user.UserId, user.FullName, user.Email, user.Role));
    }
}
