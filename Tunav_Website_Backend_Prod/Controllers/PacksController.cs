using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tunav_backend.Services;

namespace tunav_backend.Controllers;

[ApiController]
[Route("api/packs")]
public class PacksController : ControllerBase
{
    private readonly IPackService _packService;

    public PacksController(IPackService packService)
    {
        _packService = packService;
    }

    [HttpGet("catalog")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PackCatalogSolutionDto>>> GetCatalog()
    {
        return Ok(await _packService.GetCatalogAsync());
    }

    [HttpGet("general-solutions")]
    [Authorize(Policy = "SolutionWrite")]
    public async Task<ActionResult<IEnumerable<GeneralSolutionOptionDto>>> GetGeneralSolutions()
    {
        return Ok(await _packService.GetGeneralSolutionsAsync());
    }

    [HttpGet]
    [Authorize(Policy = "SolutionWrite")]
    public async Task<ActionResult<IEnumerable<PackDto>>> GetAll(
        [FromQuery] int? solutionId,
        [FromQuery] bool? isActive,
        [FromQuery] string? search)
    {
        return Ok(await _packService.GetAllAsync(solutionId, isActive, search));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "SolutionWrite")]
    public async Task<ActionResult<PackDto>> GetById(int id)
    {
        var pack = await _packService.GetByIdAsync(id);
        return pack == null
            ? NotFound(new { message = "Pack introuvable." })
            : Ok(pack);
    }

    [HttpPost]
    [Authorize(Policy = "SolutionWrite")]
    public async Task<ActionResult<PackDto>> Create([FromBody] CreatePackRequest? request)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Les donnees du pack sont obligatoires." });
        }

        try
        {
            var pack = await _packService.CreateAsync(new CreatePackDto(
                request.SolutionId,
                request.Name,
                request.Description,
                request.Features,
                request.ThemeKey,
                request.DisplayOrder,
                request.IsPopular,
                request.VideoUrl,
                request.IsActive));

            return CreatedAtAction(nameof(GetById), new { id = pack.Id }, pack);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "SolutionWrite")]
    public async Task<ActionResult<PackDto>> Update(int id, [FromBody] UpdatePackRequest? request)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Les donnees du pack sont obligatoires." });
        }

        try
        {
            var pack = await _packService.UpdateAsync(id, new UpdatePackDto(
                request.SolutionId,
                request.Name,
                request.Description,
                request.Features,
                request.ThemeKey,
                request.DisplayOrder,
                request.IsPopular,
                request.VideoUrl,
                request.IsActive));

            return pack == null
                ? NotFound(new { message = "Pack introuvable." })
                : Ok(pack);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Policy = "SolutionWrite")]
    public async Task<ActionResult<PackDto>> Toggle(int id)
    {
        var pack = await _packService.ToggleAsync(id);
        return pack == null
            ? NotFound(new { message = "Pack introuvable." })
            : Ok(pack);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "SolutionWrite")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _packService.DeleteAsync(id);
        return deleted
            ? NoContent()
            : NotFound(new { message = "Pack introuvable." });
    }
}

public record CreatePackRequest(
    int SolutionId,
    string Name,
    string Description,
    List<string>? Features,
    string? ThemeKey,
    int DisplayOrder,
    bool IsPopular,
    string? VideoUrl,
    bool IsActive = true);

public record UpdatePackRequest(
    int? SolutionId,
    string? Name,
    string? Description,
    List<string>? Features,
    string? ThemeKey,
    int? DisplayOrder,
    bool? IsPopular,
    string? VideoUrl,
    bool? IsActive);
