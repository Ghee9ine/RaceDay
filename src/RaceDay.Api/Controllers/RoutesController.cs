using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Dtos.Routes;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Race route details for a category.</summary>
[ApiController]
[Tags("Race-Day Preparation")]
public class RoutesController : ControllerBase
{
    private readonly IRouteService _routeService;

    public RoutesController(IRouteService routeService)
    {
        _routeService = routeService;
    }

    /// <summary>Returns the route details (start/end point, elevation, map) for a category.</summary>
    /// <response code="200">The route.</response>
    /// <response code="404">No route captured yet.</response>
    [HttpGet("api/categories/{categoryId:int}/route")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCategory(int categoryId)
    {
        var route = await _routeService.GetByCategoryAsync(categoryId);
        return route is null ? NotFound() : Ok(route);
    }

    /// <summary>Creates or replaces the route for a category. Owner only.</summary>
    /// <response code="200">The upserted route.</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">No such category.</response>
    [HttpPut("api/categories/{categoryId:int}/route")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upsert(int categoryId, UpsertRouteRequest request)
    {
        var result = await _routeService.UpsertAsync(categoryId, User.GetUserId(), request);
        return result.Outcome switch
        {
            ServiceOutcome.NotFound => NotFound(),
            ServiceOutcome.Forbidden => Forbid(),
            _ => Ok(result.Value),
        };
    }
}
