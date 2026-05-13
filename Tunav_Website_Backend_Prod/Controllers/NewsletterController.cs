using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;
using tunav_backend.Services;

namespace tunav_backend.Controllers;

[ApiController]
[Route("api/newsletters")]
public class NewslettersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<NewslettersController> _logger;
    private readonly INewsletterEmailService _newsletterEmail;
    private readonly IServiceScopeFactory _scopeFactory;

    public NewslettersController(
        AppDbContext db,
        ILogger<NewslettersController> logger,
        INewsletterEmailService newsletterEmail,
        IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _logger = logger;
        _newsletterEmail = newsletterEmail;
        _scopeFactory = scopeFactory;
    }

    // ── Helper : force UTC sur tout DateTime entrant ──────────────────────────
    private static DateTime ToUtc(DateTime dt) =>
        dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);

    // ── Public : liste des newsletters actives ────────────────────────────────
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] bool? active)
    {
        var q = _db.Newsletters.AsQueryable();
        if (active.HasValue)
            q = q.Where(n => n.IsActive == active.Value);

        var list = await q.OrderByDescending(n => n.PublishedAt).ToListAsync();
        return Ok(list.Select(ToDto));
    }

    [HttpGet("{id:int}"), AllowAnonymous]
    public async Task<IActionResult> GetOne(int id)
    {
        var n = await _db.Newsletters.FindAsync(id);
        return n is null ? NotFound(new { message = "Newsletter introuvable." }) : Ok(ToDto(n));
    }

    // ── Backoffice : Admin + Marketing uniquement ─────────────────────────────
    [HttpPost, Authorize(Policy = "NewsletterWrite")]
    public async Task<IActionResult> Create([FromBody] NewsletterRequest req, [FromQuery] int userId)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { message = "Le titre est obligatoire." });

        var n = new Newsletter
        {
            Title = req.Title.Trim(),
            Summary = req.Summary?.Trim(),
            TableOfContents = req.TableOfContents?.Trim(),
            CoverImageUrl = req.CoverImageUrl?.Trim(),
            PdfUrl = req.PdfUrl?.Trim(),
            PublishedAt = req.PublishedAt.HasValue ? ToUtc(req.PublishedAt.Value) : DateTime.UtcNow,
            IsActive = req.IsActive ?? true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Newsletters.Add(n);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOne), new { id = n.Id }, ToDto(n));
    }

    [HttpPut("{id:int}"), Authorize(Policy = "NewsletterWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] NewsletterRequest req)
    {
        var n = await _db.Newsletters.FindAsync(id);
        if (n is null) return NotFound(new { message = "Newsletter introuvable." });

        n.Title = req.Title?.Trim() ?? n.Title;
        n.Summary = req.Summary?.Trim() ?? n.Summary;
        n.TableOfContents = req.TableOfContents?.Trim() ?? n.TableOfContents;
        n.CoverImageUrl = req.CoverImageUrl?.Trim() ?? n.CoverImageUrl;
        n.PdfUrl = req.PdfUrl?.Trim() ?? n.PdfUrl;
        n.PublishedAt = req.PublishedAt.HasValue ? ToUtc(req.PublishedAt.Value) : n.PublishedAt;
        n.IsActive = req.IsActive ?? n.IsActive;
        n.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToDto(n));
    }

    [HttpPatch("{id:int}/toggle"), Authorize(Policy = "NewsletterWrite")]
    public async Task<IActionResult> Toggle(int id)
    {
        var n = await _db.Newsletters.FindAsync(id);
        if (n is null) return NotFound(new { message = "Newsletter introuvable." });
        n.IsActive = !n.IsActive;
        n.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(ToDto(n));
    }

    [HttpDelete("{id:int}"), Authorize(Policy = "NewsletterWrite")]
    public async Task<IActionResult> Delete(int id)
    {
        var n = await _db.Newsletters.FindAsync(id);
        if (n is null) return NotFound(new { message = "Newsletter introuvable." });
        _db.Newsletters.Remove(n);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Backoffice: notifier les abonnés d'une édition (envoi email)</summary>
    [HttpPost("{id:int}/notify"), Authorize(Policy = "NewsletterWrite")]
    public async Task<IActionResult> NotifyEdition(int id)
    {
        var exists = await _db.Newsletters.AnyAsync(n => n.Id == id);
        if (!exists) return NotFound(new { message = "Newsletter introuvable." });

        try
        {
            // Mode synchrone (robuste): on attend réellement l'envoi afin d'éviter un "fire-and-forget"
            // qui peut échouer silencieusement selon l'hébergement / redémarrages / scope.
            var result = await _newsletterEmail.NotifyEditionAsync(id, HttpContext.RequestAborted);

            return Ok(new
            {
                message = "Envoi terminé.",
                result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur notify editionId={Id}", id);
            return StatusCode(500, new { message = "Erreur lors de l'envoi de la newsletter." });
        }
    }

    // ── Upload PDF ────────────────────────────────────────────────────────────
    [HttpPost("upload-pdf"), Authorize(Policy = "NewsletterWrite")]
    public async Task<IActionResult> UploadPdf(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Aucun fichier fourni." });

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Seuls les fichiers PDF sont acceptés." });

        if (file.Length > 20 * 1024 * 1024)
            return BadRequest(new { message = "Le fichier ne doit pas dépasser 20 MB." });

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "newsletters");
        if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName);
        var safeName = Path.GetFileNameWithoutExtension(file.FileName)
            .Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
        var fileName = $"{safeName}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        return Ok(new { url = $"/uploads/newsletters/{fileName}" });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ABONNÉS — /api/newsletter-subscribers
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>S'abonner à la newsletter (public)</summary>
    [HttpPost("/api/newsletter-subscribers"), AllowAnonymous]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { message = "L'email est obligatoire." });

        var email = req.Email.Trim().ToLowerInvariant();

        // Vérifier si déjà abonné
        var existing = await _db.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == email);

        if (existing is not null)
        {
            if (existing.IsActive)
                return Conflict(new { message = "Vous êtes déjà abonné à notre newsletter." });

            // Réactiver si désabonné précédemment
            existing.IsActive = true;
            existing.SubscribedAt = DateTime.UtcNow;
            existing.Phone = req.Phone?.Trim();
            await _db.SaveChangesAsync();
            return Ok(new { message = "Votre abonnement a été réactivé avec succès !" });
        }

        var subscriber = new NewsletterSubscriber
        {
            Email = email,
            Phone = req.Phone?.Trim(),
            IsActive = true,
            SubscribedAt = DateTime.UtcNow,
            UnsubscribeToken = Guid.NewGuid().ToString("N")
        };

        _db.NewsletterSubscribers.Add(subscriber);
        await _db.SaveChangesAsync();

        // Envoyer un email de confirmation
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailSvc = scope.ServiceProvider.GetRequiredService<INewsletterEmailService>();
                await emailSvc.SendSubscriptionConfirmationEmailAsync(subscriber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur envoi confirmation newsletter {Email}", subscriber.Email);
            }
        });

        return Ok(new { message = "Merci ! Vous êtes maintenant abonné à la newsletter TUNAV." });
    }

    /// <summary>Se désabonner via token (lien dans l'email)</summary>
    [HttpGet("/api/newsletter-subscribers/unsubscribe"), AllowAnonymous]
    public async Task<IActionResult> Unsubscribe([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { message = "Token invalide." });

        var sub = await _db.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.UnsubscribeToken == token);

        if (sub is null)
            return NotFound(new { message = "Lien de désabonnement invalide." });

        sub.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Vous avez été désabonné avec succès. À bientôt !" });
    }

    /// <summary>Liste des abonnés (backoffice Admin + Marketing)</summary>
    [HttpGet("/api/newsletter-subscribers"), Authorize(Policy = "NewsletterWrite")]
    public async Task<IActionResult> GetSubscribers([FromQuery] bool? active)
    {
        var q = _db.NewsletterSubscribers.AsQueryable();
        if (active.HasValue) q = q.Where(s => s.IsActive == active.Value);
        var list = await q.OrderByDescending(s => s.SubscribedAt).ToListAsync();
        return Ok(list.Select(s => new {
            s.Id,
            s.Email,
            s.Phone,
            s.IsActive,
            s.SubscribedAt
        }));
    }

    /// <summary>Backoffice: activer/désactiver un abonné</summary>
    [HttpPatch("/api/newsletter-subscribers/{id:int}/toggle"), Authorize(Policy = "NewsletterWrite")]
    public async Task<IActionResult> ToggleSubscriber(int id)
    {
        var sub = await _db.NewsletterSubscribers.FindAsync(id);
        if (sub is null) return NotFound(new { message = "Abonné introuvable." });

        sub.IsActive = !sub.IsActive;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = sub.IsActive ? "Abonné activé." : "Abonné désactivé.",
            sub.Id,
            sub.Email,
            sub.IsActive
        });
    }

    // ── DTO ───────────────────────────────────────────────────────────────────
    private static object ToDto(Newsletter n) => new
    {
        n.Id,
        n.Title,
        n.Summary,
        n.TableOfContents,
        tocLines = n.TocLines,
        n.CoverImageUrl,
        n.PdfUrl,
        n.PublishedAt,
        n.IsActive,
        n.CreatedAt,
        n.UpdatedAt
    };
}

public record NewsletterRequest(
    string? Title,
    string? Summary,
    string? TableOfContents,
    string? CoverImageUrl,
    string? PdfUrl,
    DateTime? PublishedAt,
    bool? IsActive);

public record SubscribeRequest(
    string Email,
    string? Phone);