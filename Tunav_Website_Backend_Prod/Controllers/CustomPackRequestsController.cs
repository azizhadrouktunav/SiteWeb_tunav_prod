using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tunav_backend.Services;

namespace tunav_backend.Controllers;

[ApiController]
[Route("api/custom-pack-requests")]
public class CustomPackRequestsController : ControllerBase
{
    private readonly ICustomPackRequestService _customPackRequestService;
    private readonly IPackService _packService;

    public CustomPackRequestsController(
        ICustomPackRequestService customPackRequestService,
        IPackService packService)
    {
        _customPackRequestService = customPackRequestService;
        _packService = packService;
    }

    [HttpGet("general-solutions")]
    [Authorize(Policy = "CollaborationRead")]
    public async Task<ActionResult<IEnumerable<GeneralSolutionOptionDto>>> GetGeneralSolutions()
    {
        return Ok(await _packService.GetGeneralSolutionsAsync());
    }

    [HttpGet]
    [Authorize(Policy = "CollaborationRead")]
    public async Task<ActionResult<IEnumerable<CustomPackRequestDto>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? solutionId,
        [FromQuery] string? search)
    {
        return Ok(await _customPackRequestService.GetAllAsync(status, solutionId, search));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "CollaborationRead")]
    public async Task<ActionResult<CustomPackRequestDto>> GetById(int id)
    {
        var item = await _customPackRequestService.GetByIdAsync(id);
        return item == null
            ? NotFound(new { message = "Demande de pack personnalisée introuvable." })
            : Ok(item);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Submit([FromBody] SubmitCustomPackRequest? request)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Les donnees de la demande sont obligatoires." });
        }

        try
        {
            var item = await _customPackRequestService.CreateAsync(new CreateCustomPackRequestDto(
                request.SolutionId,
                request.ContactName,
                request.Company,
                request.Email,
                request.Phone,
                request.Message,
                request.SelectedFeatures));

            return Ok(new
            {
                message = "Votre demande de pack personnalisé a bien été envoyée.",
                requestId = item.Id,
                data = item
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "CollaborationRead")]
    public async Task<ActionResult<CustomPackRequestDto>> UpdateStatus(int id, [FromBody] UpdateCustomPackRequestStatus? request)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Les donnees de statut sont obligatoires." });
        }

        try
        {
            var item = await _customPackRequestService.UpdateStatusAsync(id, request.Status, request.InternalNote);
            return item == null
                ? NotFound(new { message = "Demande de pack personnalisée introuvable." })
                : Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CollaborationRead")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _customPackRequestService.DeleteAsync(id);
        return deleted
            ? NoContent()
            : NotFound(new { message = "Demande de pack personnalisée introuvable." });
    }
}

public record SubmitCustomPackRequest(
    int SolutionId,
    string ContactName,
    string Company,
    string Email,
    string Phone,
    string? Message,
    List<string>? SelectedFeatures);

public record UpdateCustomPackRequestStatus(
    string Status,
    string? InternalNote);
