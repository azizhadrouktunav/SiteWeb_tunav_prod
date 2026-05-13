using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tunav_backend.DTOs;
using tunav_backend.Services;

namespace tunav_backend.Controllers;

/// <summary>
/// API Gestion de l'équipe — Module Présentation de l'Entreprise
/// US-PE08: Ajout | US-PE09: Modification | US-PE10: Suppression | US-PE11: Activation/Désactivation
/// </summary>
[ApiController]
[Route("api/team-members")]
public class TeamMembersController : ControllerBase
{
    private readonly ITeamMemberService _service;

    public TeamMembersController(ITeamMemberService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET tous les membres — public: ?isActiveOnly=true | backoffice: tous
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TeamMemberDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TeamMemberDto>>> GetAll(
        [FromQuery] bool? isActiveOnly = null)
    {
        var members = await _service.GetAllAsync(isActiveOnly);
        return Ok(members);
    }

    /// <summary>
    /// GET un membre par ID
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamMemberDto>> GetById(int id)
    {
        var member = await _service.GetByIdAsync(id);
        if (member == null)
            return NotFound(new { message = $"Membre avec l'ID {id} introuvable." });

        return Ok(member);
    }

    /// <summary>
    /// POST — US-PE08 : Ajout d'un membre de l'équipe
    /// </summary>
    [Authorize(Policy = "TeamWrite")]
    [HttpPost]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeamMemberDto>> Create(
        [FromBody] CreateTeamMemberDto dto,
        [FromQuery] int userId = 1)
    {
        if (dto == null)
            return BadRequest(new { message = "Le corps de la requête est obligatoire." });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var member = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = member.Id }, member);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT — US-PE09 : Modification d'un membre (nom, poste, photo, description)
    /// </summary>
    [Authorize(Policy = "TeamWrite")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeamMemberDto>> Update(
        int id,
        [FromBody] UpdateTeamMemberDto dto)
    {
        if (dto == null)
            return BadRequest(new { message = "Le corps de la requête est obligatoire." });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var member = await _service.UpdateAsync(id, dto);
            return Ok(member);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PATCH — US-PE11 : Activation / Désactivation d'un membre
    /// </summary>
    [Authorize(Policy = "TeamWrite")]
    [HttpPatch("{id:int}/toggle")]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamMemberDto>> Toggle(int id)
    {
        try
        {
            var member = await _service.ToggleActiveAsync(id);
            return Ok(new
            {
                message = member.IsActive
                    ? $"{member.FullName} est maintenant visible sur le site."
                    : $"{member.FullName} est maintenant masqué du site.",
                member
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// DELETE — US-PE10 : Suppression d'un membre de l'équipe
    /// </summary>
    [Authorize(Policy = "TeamWrite")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new { message = $"Membre avec l'ID {id} supprimé avec succès." });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}