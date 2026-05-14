using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using tunav_backend.DTOs;
using tunav_backend.Services;

namespace tunav_backend.Controllers;

/// <summary>
/// Solutions API Controller
/// Manages solution CRUD operations for marketing staff
/// Module: US-GS09 - Gestion des solutions Générale ou Sectorielle
/// </summary>
[ApiController]
[Route("api/solutions")]
public class SolutionsController : ControllerBase
{
    private readonly ISolutionService _solutionService;

    public SolutionsController(ISolutionService solutionService)
    {
        _solutionService = solutionService;
    }

    /// <summary>
    /// Get all solutions with optional filters
    /// </summary>
    /// <param name="isActiveOnly">Filter by active status (optional)</param>
    /// <param name="isActive">Filter by exact status (optional)</param>
    /// <param name="type">Filter by type: 'General' or 'Sectorial' (optional)</param>
    /// <param name="sector">Filter by sector (optional)</param>
    /// <param name="search">Search by title/description (optional)</param>
    /// <returns>List of solutions</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SolutionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SolutionDto>>> GetAllSolutions(
        [FromQuery] bool? isActiveOnly = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? type = null,
        [FromQuery] string? sector = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var solutions = await _solutionService.GetAllSolutionsAsync(isActiveOnly, isActive, type, sector, search);
            return Ok(solutions);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get allowed sector values for solution filters/forms
    /// </summary>
    [HttpGet("sectors")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> GetAllowedSectors()
    {
        return Ok(await _solutionService.GetAvailableSectorsAsync());
    }

    /// <summary>
    /// Get a specific solution by ID
    /// </summary>
    /// <param name="id">Solution ID</param>
    /// <returns>The solution details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SolutionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SolutionDto>> GetSolutionById(int id)
    {
        var solution = await _solutionService.GetSolutionByIdAsync(id);
        if (solution == null)
        {
            return NotFound(new { message = $"Solution with ID {id} not found." });
        }

        return Ok(solution);
    }

    /// <summary>
    /// Get solutions filtered by type
    /// </summary>
    /// <param name="type">Solution type: 'General' or 'Sectorial'</param>
    /// <returns>List of solutions of specified type</returns>
    [HttpGet("type/{type}")]
    [ProducesResponseType(typeof(IEnumerable<SolutionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<SolutionDto>>> GetSolutionsByType(string type)
    {
        try
        {
            var solutions = await _solutionService.GetSolutionsByTypeAsync(type);
            return Ok(solutions);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create a new solution
    /// Requires: Marketing role or permissions.manage permission
    /// </summary>
    /// <param name="dto">Solution data</param>
    /// <returns>The created solution</returns>
    [HttpPost]
    [Authorize(Policy = "SolutionWrite")]
    [ProducesResponseType(typeof(SolutionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SolutionDto>> CreateSolution([FromBody] CreateSolutionDto dto)
    {
        if (dto == null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetAuthenticatedUserId();
            if (userId <= 0)
            {
                return Unauthorized(new { message = "Authenticated user ID is required." });
            }

            var solution = await _solutionService.CreateSolutionAsync(dto, userId);
            return CreatedAtAction(nameof(GetSolutionById), new { id = solution.Id }, solution);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing solution
    /// Requires: Marketing role or permissions.manage permission
    /// </summary>
    /// <param name="id">Solution ID</param>
    /// <param name="dto">Updated solution data</param>
    /// <returns>The updated solution</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "SolutionWrite")]
    [ProducesResponseType(typeof(SolutionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SolutionDto>> UpdateSolution(int id, [FromBody] UpdateSolutionDto dto)
    {
        if (dto == null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var solution = await _solutionService.UpdateSolutionAsync(id, dto);
            return Ok(solution);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Toggle solution active status
    /// Requires: Marketing role or permissions.manage permission
    /// </summary>
    /// <param name="id">Solution ID</param>
    /// <returns>The updated solution</returns>
    [HttpPatch("{id:int}/toggle")]
    [Authorize(Policy = "SolutionWrite")]
    [ProducesResponseType(typeof(SolutionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SolutionDto>> ToggleSolutionStatus(int id)
    {
        try
        {
            var solution = await _solutionService.GetSolutionByIdAsync(id);
            if (solution == null)
            {
                return NotFound(new { message = $"Solution with ID {id} does not exist." });
            }

            var updated = await _solutionService.UpdateSolutionAsync(id, new UpdateSolutionDto
            {
                IsActive = !solution.IsActive
            });

            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a solution permanently
    /// Requires: Marketing role or permissions.manage permission
    /// </summary>
    /// <param name="id">Solution ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "SolutionWrite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> DeleteSolution(int id)
    {
        try
        {
            var success = await _solutionService.DeleteSolutionAsync(id);
            if (success)
            {
                return Ok(new { message = $"Solution with ID {id} has been deleted permanently." });
            }
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            return BadRequest(new
            {
                message = "Impossible de supprimer cette solution (données liées en base). Détail technique : "
                          + ex.InnerException?.Message ?? ex.Message
            });
        }

        return BadRequest(new { message = "Failed to delete solution." });
    }

    private int GetAuthenticatedUserId()
    {
        var userIdValue = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdValue, out var userId) ? userId : 0;
    }
}
