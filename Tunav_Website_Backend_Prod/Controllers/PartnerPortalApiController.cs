using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Controllers;

/// <summary>
/// API REST – Portail Partenaire
/// Base URL : /api/partner-portal
///
/// Les partenaires backoffice sont des Users avec rôle "Partenaire".
/// On utilise UserId (clé vers la table users) — pas de lien avec la table partners (vitrine).
/// </summary>
[ApiController]
[Route("api/partner-portal")]
[Authorize] // JWT obligatoire sur tout le controller
public class PartnerPortalApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public PartnerPortalApiController(AppDbContext context)
    {
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RÉCLAMATIONS (PartnerClaim)  —  gérées par SAV
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Liste toutes les réclamations (vue SAV) — filtres optionnels</summary>
    /// GET /api/partner-portal/claims?status=Nouvelle&amp;priority=Haute&amp;userId=6&amp;page=1&amp;pageSize=20
    [Authorize(Policy = "SavAccess")]
    [HttpGet("claims")]
    public async Task<IActionResult> GetClaims(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.PartnerClaims
            .Include(c => c.User)
            .Include(c => c.AssignedToUser)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(c => c.Priority == priority);

        if (userId.HasValue)
            query = query.Where(c => c.UserId == userId.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.Subject,
                c.Priority,
                c.Status,
                c.SavNote,
                c.CreatedAt,
                c.UpdatedAt,
                c.ResolvedAt,
                PartnerUser = new
                {
                    c.User.Id,
                    FullName = c.User.FirstName + " " + c.User.LastName,
                    c.User.Email
                },
                AssignedTo = c.AssignedToUser == null ? null : new
                {
                    c.AssignedToUser.Id,
                    FullName = c.AssignedToUser.FirstName + " " + c.AssignedToUser.LastName
                }
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>Réclamations d'un partenaire connecté (dashboard partenaire)</summary>
    /// GET /api/partner-portal/claims/by-user/{userId}
    [Authorize(Policy = "PartnerPortalAccess")]
    [HttpGet("claims/by-user/{userId:int}")]
    public async Task<IActionResult> GetClaimsByUser(int userId)
    {
        var userExists = await _context.Users
            .Include(u => u.Role)
            .AnyAsync(u => u.Id == userId && u.Role!.Name == "Partenaire");

        if (!userExists)
            return NotFound(new { message = "Utilisateur partenaire introuvable." });

        var claims = await _context.PartnerClaims
            .Where(c => c.UserId == userId)
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Subject,
                c.Description,
                c.Priority,
                c.Status,
                c.SavNote,
                c.CreatedAt,
                c.UpdatedAt,
                c.ResolvedAt
            })
            .ToListAsync();

        return Ok(claims);
    }

    /// <summary>Détail d'une réclamation</summary>
    /// GET /api/partner-portal/claims/{id}
    [Authorize(Policy = "SavAccess")]
    [HttpGet("claims/{id:int}")]
    public async Task<IActionResult> GetClaim(int id)
    {
        var claim = await _context.PartnerClaims
            .Include(c => c.User)
            .Include(c => c.AssignedToUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (claim == null)
            return NotFound(new { message = "Réclamation introuvable." });

        return Ok(new
        {
            claim.Id,
            claim.Subject,
            claim.Description,
            claim.Priority,
            claim.Status,
            claim.SavNote,
            claim.CreatedAt,
            claim.UpdatedAt,
            claim.ResolvedAt,
            PartnerUser = new
            {
                claim.User.Id,
                FullName = claim.User.FirstName + " " + claim.User.LastName,
                claim.User.Email
            },
            AssignedTo = claim.AssignedToUser == null ? null : new
            {
                claim.AssignedToUser.Id,
                FullName = claim.AssignedToUser.FirstName + " " + claim.AssignedToUser.LastName
            }
        });
    }

    /// <summary>Partenaire soumet une nouvelle réclamation</summary>
    /// POST /api/partner-portal/claims
    [Authorize(Policy = "PartnerPortalAccess")]
    [HttpPost("claims")]
    public async Task<IActionResult> CreateClaim([FromBody] CreateClaimDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == dto.UserId);

        if (user == null)
            return BadRequest(new { message = "Utilisateur introuvable." });

        if (user.Role?.Name != "Partenaire")
            return BadRequest(new { message = "Cet utilisateur n'a pas le rôle Partenaire." });

        if (!user.IsActive)
            return BadRequest(new { message = "Ce compte est désactivé." });

        var claim = new PartnerClaim
        {
            UserId = dto.UserId,
            Subject = dto.Subject.Trim(),
            Description = dto.Description.Trim(),
            Priority = dto.Priority ?? "Normale",
            Status = "Nouvelle",
            CreatedAt = DateTime.UtcNow
        };

        _context.PartnerClaims.Add(claim);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetClaim), new { id = claim.Id },
            new { claim.Id, message = "Réclamation soumise avec succès." });
    }

    /// <summary>SAV traite une réclamation (mise à jour statut + note)</summary>
    /// PATCH /api/partner-portal/claims/{id}/treat
    [Authorize(Policy = "SavAccess")]
    [HttpPatch("claims/{id:int}/treat")]
    public async Task<IActionResult> TreatClaim(int id, [FromBody] TreatClaimDto dto)
    {
        var claim = await _context.PartnerClaims.FindAsync(id);
        if (claim == null)
            return NotFound(new { message = "Réclamation introuvable." });

        claim.Status = dto.Status ?? claim.Status;
        claim.SavNote = dto.SavNote ?? claim.SavNote;
        claim.AssignedToUserId = dto.AssignedToUserId ?? claim.AssignedToUserId;
        claim.UpdatedAt = DateTime.UtcNow;

        if (dto.Status is "Résolue" or "Fermée")
            claim.ResolvedAt ??= DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Réclamation mise à jour.", claim.Status });
    }

    /// <summary>Supprime une réclamation (admin seulement)</summary>
    /// DELETE /api/partner-portal/claims/{id}
    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("claims/{id:int}")]
    public async Task<IActionResult> DeleteClaim(int id)
    {
        var claim = await _context.PartnerClaims.FindAsync(id);
        if (claim == null)
            return NotFound(new { message = "Réclamation introuvable." });

        _context.PartnerClaims.Remove(claim);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Réclamation supprimée." });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DEMANDES (PartnerDemand)  —  gérées par Commercial
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Liste toutes les demandes (vue Commercial) — filtres optionnels</summary>
    /// GET /api/partner-portal/demands?status=Nouvelle&amp;demandType=Démonstration&amp;userId=6&amp;page=1&amp;pageSize=20
    [Authorize(Policy = "CommercialDemandsAccess")]
    [HttpGet("demands")]
    public async Task<IActionResult> GetDemands(
        [FromQuery] string? status,
        [FromQuery] string? demandType,
        [FromQuery] int? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.PartnerDemands
            .Include(d => d.User)
            .Include(d => d.AssignedToUser)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(d => d.Status == status);

        if (!string.IsNullOrWhiteSpace(demandType))
            query = query.Where(d => d.DemandType == demandType);

        if (userId.HasValue)
            query = query.Where(d => d.UserId == userId.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                d.Id,
                d.DemandType,
                d.Subject,
                d.Status,
                d.CommercialNote,
                d.AttachmentUrl,
                d.CreatedAt,
                d.UpdatedAt,
                d.ClosedAt,
                PartnerUser = new
                {
                    d.User.Id,
                    FullName = d.User.FirstName + " " + d.User.LastName,
                    d.User.Email
                },
                AssignedTo = d.AssignedToUser == null ? null : new
                {
                    d.AssignedToUser.Id,
                    FullName = d.AssignedToUser.FirstName + " " + d.AssignedToUser.LastName
                }
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>Demandes d'un partenaire connecté (dashboard partenaire)</summary>
    /// GET /api/partner-portal/demands/by-user/{userId}
    [Authorize(Policy = "PartnerPortalAccess")]
    [HttpGet("demands/by-user/{userId:int}")]
    public async Task<IActionResult> GetDemandsByUser(int userId)
    {
        var userExists = await _context.Users
            .Include(u => u.Role)
            .AnyAsync(u => u.Id == userId && u.Role!.Name == "Partenaire");

        if (!userExists)
            return NotFound(new { message = "Utilisateur partenaire introuvable." });

        var demands = await _context.PartnerDemands
            .Where(d => d.UserId == userId)
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new
            {
                d.Id,
                d.DemandType,
                d.Subject,
                d.Description,
                d.Status,
                d.CommercialNote,
                d.AttachmentUrl,
                d.CreatedAt,
                d.UpdatedAt,
                d.ClosedAt
            })
            .ToListAsync();

        return Ok(demands);
    }

    /// <summary>Détail d'une demande</summary>
    /// GET /api/partner-portal/demands/{id}
    [Authorize(Policy = "CommercialDemandsAccess")]
    [HttpGet("demands/{id:int}")]
    public async Task<IActionResult> GetDemand(int id)
    {
        var demand = await _context.PartnerDemands
            .Include(d => d.User)
            .Include(d => d.AssignedToUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (demand == null)
            return NotFound(new { message = "Demande introuvable." });

        return Ok(new
        {
            demand.Id,
            demand.DemandType,
            demand.Subject,
            demand.Description,
            demand.AttachmentUrl,
            demand.Status,
            demand.CommercialNote,
            demand.CreatedAt,
            demand.UpdatedAt,
            demand.ClosedAt,
            PartnerUser = new
            {
                demand.User.Id,
                FullName = demand.User.FirstName + " " + demand.User.LastName,
                demand.User.Email
            },
            AssignedTo = demand.AssignedToUser == null ? null : new
            {
                demand.AssignedToUser.Id,
                FullName = demand.AssignedToUser.FirstName + " " + demand.AssignedToUser.LastName
            }
        });
    }

    /// <summary>Partenaire soumet une nouvelle demande</summary>
    /// POST /api/partner-portal/demands
    [Authorize(Policy = "PartnerPortalAccess")]
    [HttpPost("demands")]
    public async Task<IActionResult> CreateDemand([FromBody] CreateDemandDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == dto.UserId);

        if (user == null)
            return BadRequest(new { message = "Utilisateur introuvable." });

        if (user.Role?.Name != "Partenaire")
            return BadRequest(new { message = "Cet utilisateur n'a pas le rôle Partenaire." });

        if (!user.IsActive)
            return BadRequest(new { message = "Ce compte est désactivé." });

        var demand = new PartnerDemand
        {
            UserId = dto.UserId,
            DemandType = dto.DemandType.Trim(),
            Subject = dto.Subject.Trim(),
            Description = dto.Description.Trim(),
            AttachmentUrl = dto.AttachmentUrl?.Trim(),
            Status = "Nouvelle",
            CreatedAt = DateTime.UtcNow
        };

        _context.PartnerDemands.Add(demand);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDemand), new { id = demand.Id },
            new { demand.Id, message = "Demande soumise avec succès." });
    }

    /// <summary>Commercial traite une demande (mise à jour statut + note)</summary>
    /// PATCH /api/partner-portal/demands/{id}/treat
    [Authorize(Policy = "CommercialDemandsAccess")]
    [HttpPatch("demands/{id:int}/treat")]
    public async Task<IActionResult> TreatDemand(int id, [FromBody] TreatDemandDto dto)
    {
        var demand = await _context.PartnerDemands.FindAsync(id);
        if (demand == null)
            return NotFound(new { message = "Demande introuvable." });

        demand.Status = dto.Status ?? demand.Status;
        demand.CommercialNote = dto.CommercialNote ?? demand.CommercialNote;
        demand.AssignedToUserId = dto.AssignedToUserId ?? demand.AssignedToUserId;
        demand.UpdatedAt = DateTime.UtcNow;

        if (dto.Status is "Clôturée" or "Refusée" or "Acceptée")
            demand.ClosedAt ??= DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Demande mise à jour.", demand.Status });
    }

    /// <summary>Supprime une demande (admin seulement)</summary>
    /// DELETE /api/partner-portal/demands/{id}
    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("demands/{id:int}")]
    public async Task<IActionResult> DeleteDemand(int id)
    {
        var demand = await _context.PartnerDemands.FindAsync(id);
        if (demand == null)
            return NotFound(new { message = "Demande introuvable." });

        _context.PartnerDemands.Remove(demand);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Demande supprimée." });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // STATISTIQUES  —  KPIs dashboard
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Statistiques globales (SAV + Commercial)</summary>
    /// GET /api/partner-portal/stats
    [Authorize(Policy = "SavAccess")]
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var claimsStats = await _context.PartnerClaims
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var demandsStats = await _context.PartnerDemands
            .GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var claimsByPriority = await _context.PartnerClaims
            .Where(c => c.Status != "Fermée" && c.Status != "Résolue")
            .GroupBy(c => c.Priority)
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(new
        {
            claims = new
            {
                byStatus = claimsStats,
                byPriority = claimsByPriority,
                total = claimsStats.Sum(s => s.Count)
            },
            demands = new
            {
                byStatus = demandsStats,
                total = demandsStats.Sum(s => s.Count)
            }
        });
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DTOs  —  UserId remplace PartnerId
// ═══════════════════════════════════════════════════════════════════════════════

public record CreateClaimDto(
    int UserId,
    string Subject,
    string Description,
    string? Priority         // "Basse" | "Normale" | "Haute" | "Urgente"
);

public record TreatClaimDto(
    string? Status,          // "En cours" | "Résolue" | "Fermée"
    string? SavNote,
    int? AssignedToUserId
);

public record CreateDemandDto(
    int UserId,
    string DemandType,      // "Nouveau client" | "Démonstration" | "Support commercial" | "Autre"
    string Subject,
    string Description,
    string? AttachmentUrl
);

public record TreatDemandDto(
    string? Status,          // "En traitement" | "Acceptée" | "Refusée" | "Clôturée"
    string? CommercialNote,
    int? AssignedToUserId
);