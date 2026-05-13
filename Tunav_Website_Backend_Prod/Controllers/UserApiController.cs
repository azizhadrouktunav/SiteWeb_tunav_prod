using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Controllers;

[Authorize(Policy = "ManageUsers")]
[ApiController]
[Route("api/users")]
public class UserApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserApiController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Récupère tous les utilisateurs. Filtre optionnel sur IsActive.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAll([FromQuery] bool? isActive)
    {
        var query = _context.Users.Include(u => u.Role).AsQueryable();

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var list = await query
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Récupère un utilisateur par son Id.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<User>> GetById(int id)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null) return NotFound(new { message = $"Utilisateur avec l'id {id} introuvable." });
        return Ok(user);
    }

    /// <summary>
    /// Crée un nouvel utilisateur — US-A03 + US-A05
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<User>> Create([FromBody] User model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
        if (emailExists)
            return Conflict(new { message = $"Un utilisateur avec l'email '{model.Email}' existe déjà." });

        var roleExists = await _context.Roles.AnyAsync(r => r.Id == model.RoleId);
        if (!roleExists)
            return BadRequest(new { message = $"Le rôle avec l'id {model.RoleId} n'existe pas." });

        model.Id = 0;
        model.CreatedAt = DateTime.UtcNow;
        model.IsActive = true;
        model.UpdatedAt = null;
        model.PasswordHash = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(model.PasswordHash));

        _context.Users.Add(model);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
    }

    /// <summary>
    /// Met à jour un utilisateur existant — US-A03
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<User>> Update(int id, [FromBody] User model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = await _context.Users.FindAsync(id);
        if (existing is null)
            return NotFound(new { message = $"Utilisateur avec l'id {id} introuvable." });

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == model.Email && u.Id != id);
        if (emailExists)
            return Conflict(new { message = $"Un utilisateur avec l'email '{model.Email}' existe déjà." });

        var roleExists = await _context.Roles.AnyAsync(r => r.Id == model.RoleId);
        if (!roleExists)
            return BadRequest(new { message = $"Le rôle avec l'id {model.RoleId} n'existe pas." });

        existing.FirstName = model.FirstName;
        existing.LastName = model.LastName;
        existing.Email = model.Email;
        existing.RoleId = model.RoleId;
        existing.IsActive = model.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(model.PasswordHash))
        {
            existing.PasswordHash = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(model.PasswordHash));
        }

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    /// <summary>
    /// US-A05 — Attribution d'un rôle à un utilisateur existant
    /// </summary>
    [HttpPatch("{id:int}/assign-role")]
    public async Task<ActionResult> AssignRole(int id, [FromBody] AssignRoleRequest request)
    {
        var existing = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (existing is null)
            return NotFound(new { message = $"Utilisateur avec l'id {id} introuvable." });

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId);
        if (role is null)
            return NotFound(new { message = $"Rôle avec l'id {request.RoleId} introuvable." });

        if (!role.IsActive)
            return BadRequest(new { message = $"Le rôle '{role.Name}' est désactivé. Choisissez un rôle actif." });

        var ancienRole = existing.Role?.Name ?? "Aucun";
        existing.RoleId = request.RoleId;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"Le rôle de '{existing.FirstName} {existing.LastName}' a été changé de '{ancienRole}' vers '{role.Name}'.",
            userId = existing.Id,
            newRoleId = role.Id,
            newRoleName = role.Name
        });
    }

    /// <summary>
    /// US-A04 — Active ou désactive un compte utilisateur (Toggle IsActive)
    /// </summary>
    [HttpPatch("{id:int}/toggle")]
    public async Task<ActionResult> Toggle(int id)
    {
        var existing = await _context.Users.FindAsync(id);
        if (existing is null)
            return NotFound(new { message = $"Utilisateur avec l'id {id} introuvable." });

        existing.IsActive = !existing.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = existing.IsActive
                ? $"Le compte de '{existing.FirstName} {existing.LastName}' a été activé."
                : $"Le compte de '{existing.FirstName} {existing.LastName}' a été désactivé.",
            isActive = existing.IsActive,
            userId = existing.Id
        });
    }

    /// <summary>
    /// US-A08 — Réinitialisation du mot de passe par l'Admin
    /// </summary>
    [HttpPatch("{id:int}/reset-password")]
    public async Task<ActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Le nouveau mot de passe est obligatoire." });

        if (request.NewPassword.Length < 6)
            return BadRequest(new { message = "Le mot de passe doit contenir au moins 6 caractères." });

        var existing = await _context.Users.FindAsync(id);
        if (existing is null)
            return NotFound(new { message = $"Utilisateur avec l'id {id} introuvable." });

        if (!existing.IsActive)
            return BadRequest(new { message = "Impossible de réinitialiser le mot de passe d'un compte désactivé." });

        existing.PasswordHash = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(request.NewPassword));
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"Le mot de passe de '{existing.FirstName} {existing.LastName}' a été réinitialisé avec succès.",
            userId = existing.Id
        });
    }

    /// <summary>
    /// Supprime physiquement un utilisateur — US-A03
    /// Vérifie les dépendances FK avant suppression pour éviter l'erreur 23503.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _context.Users.FindAsync(id);
        if (existing is null)
            return NotFound(new { message = $"Utilisateur avec l'id {id} introuvable." });

        // ── Vérification des clés étrangères ────────────────────────────────
        var hasEvents = await _context.Events
            .AnyAsync(e => e.CreatedBy == id);
        if (hasEvents)
            return Conflict(new
            {
                message = $"Impossible de supprimer '{existing.FirstName} {existing.LastName}' : " +
                          "cet utilisateur a créé des événements. Désactivez-le à la place (bouton Activer/Désactiver) " +
                          "ou supprimez d'abord ses événements."
            });

        var hasSolutions = await _context.Solutions
            .AnyAsync(s => s.CreatedBy == id);
        if (hasSolutions)
            return Conflict(new
            {
                message = $"Impossible de supprimer '{existing.FirstName} {existing.LastName}' : " +
                          "cet utilisateur a créé des solutions. Désactivez-le à la place."
            });

        var hasTeamMembers = await _context.TeamMembers
            .AnyAsync(t => t.CreatedBy == id);
        if (hasTeamMembers)
            return Conflict(new
            {
                message = $"Impossible de supprimer '{existing.FirstName} {existing.LastName}' : " +
                          "cet utilisateur a créé des membres d'équipe. Désactivez-le à la place."
            });

        var hasJobOffers = await _context.JobOffers
            .AnyAsync(j => j.CreatedBy == id);
        if (hasJobOffers)
            return Conflict(new
            {
                message = $"Impossible de supprimer '{existing.FirstName} {existing.LastName}' : " +
                          "cet utilisateur a créé des offres d'emploi. Désactivez-le à la place."
            });
        // ────────────────────────────────────────────────────────────────────

        _context.Users.Remove(existing);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}