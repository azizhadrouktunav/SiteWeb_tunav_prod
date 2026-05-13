using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Controllers;

[Authorize(Policy = "ManageRoles")]
[ApiController]
[Route("api/roles")]
public class RoleApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public RoleApiController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Récupère tous les rôles. Filtre optionnel sur IsActive.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Role>>> GetAllRolesActive([FromQuery] bool? isActive)
    {
        var query = _context.Roles.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        var list = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Récupère un rôle par son Id.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Role>> GetById(int id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role is null) return NotFound($"Rôle avec l'id {id} introuvable.");
        return Ok(role);
    }

    /// <summary>
    /// Crée ou met à jour un rôle.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Role>> EditRole([FromBody] Role model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var nameExists = await _context.Roles
            .AnyAsync(r => r.Name == model.Name && r.Id != model.Id);

        if (nameExists)
            return Conflict(new { message = $"Un rôle avec le nom '{model.Name}' existe déjà." });

        if (model.Id == 0)
        {
            model.CreatedAt = DateTime.UtcNow;
            model.IsActive = true;
            model.UpdatedAt = null;

            _context.Roles.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }
        else
        {
            var existing = await _context.Roles.FindAsync(model.Id);
            if (existing is null)
                return NotFound($"Rôle avec l'id {model.Id} introuvable.");

            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.IsActive = model.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }
    }

    /// <summary>
    /// Supprime physiquement un rôle par son Id.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRole(int id)
    {
        var existing = await _context.Roles.FindAsync(id);
        if (existing is null)
            return NotFound($"Rôle avec l'id {id} introuvable.");

        _context.Roles.Remove(existing);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}