using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Dtos.Categories;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Distance/age categories offered under an event.</summary>
[ApiController]
[Tags("Categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>Lists all categories offered under a specific event.</summary>
    /// <response code="200">The event's categories.</response>
    /// <response code="404">No such event.</response>
    [HttpGet("api/events/{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEvent(int eventId)
    {
        var categories = await _categoryService.GetByEventAsync(eventId);
        return categories is null ? NotFound() : Ok(categories);
    }

    /// <summary>Returns details for a single category.</summary>
    /// <response code="200">The category.</response>
    /// <response code="404">No such category.</response>
    [HttpGet("api/categories/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        return category is null ? NotFound() : Ok(category);
    }

    /// <summary>Adds a new category to an event. Owner only.</summary>
    /// <response code="201">The new category.</response>
    /// <response code="400">Validation failed, including a duplicate name for this event.</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">No such event.</response>
    [HttpPost("api/events/{eventId:int}/categories")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CategoryRequest request)
    {
        var result = await _categoryService.CreateAsync(eventId, User.GetUserId(), request);
        return result.Outcome switch
        {
            ServiceOutcome.NotFound => NotFound(),
            ServiceOutcome.Forbidden => Forbid(),
            ServiceOutcome.ValidationFailed => BadRequest(new { message = result.Error }),
            _ => CreatedAtAction(nameof(GetById), new { id = result.Value!.CategoryId }, result.Value),
        };
    }

    /// <summary>Updates a category's details. Owner only.</summary>
    /// <response code="200">The updated category.</response>
    /// <response code="400">Validation failed, including a duplicate name for this event.</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">No such category.</response>
    [HttpPut("api/categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var result = await _categoryService.UpdateAsync(id, User.GetUserId(), request);
        return result.Outcome switch
        {
            ServiceOutcome.NotFound => NotFound(),
            ServiceOutcome.Forbidden => Forbid(),
            ServiceOutcome.ValidationFailed => BadRequest(new { message = result.Error }),
            _ => Ok(result.Value),
        };
    }

    /// <summary>Removes a category and its route, provided no enrolments exist for it. Owner only.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">No such category.</response>
    /// <response code="409">The category already has enrolments.</response>
    [HttpDelete("api/categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _categoryService.DeleteAsync(id, User.GetUserId());
        return result.Outcome switch
        {
            ServiceOutcome.NotFound => NotFound(),
            ServiceOutcome.Forbidden => Forbid(),
            ServiceOutcome.Conflict => Conflict(new { message = "This category already has enrolments and can't be deleted." }),
            _ => NoContent(),
        };
    }
}
