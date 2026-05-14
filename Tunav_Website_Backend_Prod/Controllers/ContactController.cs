using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    /// <summary>Sujet des demandes créées via le modal rendez-vous (page Contact publique).</summary>
    private const string DemoAppointmentSubject = "Demande de démonstration";

    private readonly AppDbContext _db;
    public ContactController(AppDbContext db) => _db = db;

    // ── POST /api/contact  (public — formulaire contact) ─────────────────────
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] ContactRequest dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var entity = new ContactRequest
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim().ToLower(),
            Phone = dto.Phone?.Trim() ?? "",
            Company = dto.Company?.Trim(),
            Subject = dto.Subject.Trim(),
            OtherSubject = dto.OtherSubject?.Trim(),
            Message = dto.Message.Trim(),
            Consent = dto.Consent,
            Status = "Nouveau",
            CreatedAt = DateTime.UtcNow
        };

        _db.ContactRequests.Add(entity);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Votre demande a bien été envoyée." });
    }

    // ── POST /api/contact/demo  (public — modal rendez-vous page Contact) ─────
    [HttpPost("demo")]
    public async Task<IActionResult> SubmitDemo([FromBody] ContactDemoSubmitDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var first = dto.FirstName.Trim();
        var last = dto.LastName.Trim();
        var email = dto.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last) || string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Les champs nom, prénom et e-mail sont obligatoires." });

        if (dto.HasWhatsApp && string.IsNullOrWhiteSpace(dto.WhatsAppNumber))
            return BadRequest(new { message = "Veuillez saisir votre numéro WhatsApp." });

        var wa = dto.HasWhatsApp ? dto.WhatsAppNumber!.Trim() : null;
        var phoneForRow = dto.HasWhatsApp && !string.IsNullOrEmpty(wa)
            ? (wa.Length > 20 ? wa[..20] : wa)
            : "";

        var message =
            "Demande de rendez-vous / démonstration envoyée depuis la page Contact.\n\n" +
            $"WhatsApp : {(dto.HasWhatsApp ? "Oui" : "Non")}\n" +
            (dto.HasWhatsApp && !string.IsNullOrEmpty(wa) ? $"Numéro WhatsApp : {wa}\n" : "");

        var entity = new ContactRequest
        {
            FirstName = first,
            LastName = last,
            Email = email,
            Phone = phoneForRow,
            Company = null,
            Subject = DemoAppointmentSubject,
            OtherSubject = null,
            Message = message.TrimEnd(),
            Consent = true,
            Status = "Nouveau",
            CreatedAt = DateTime.UtcNow
        };

        _db.ContactRequests.Add(entity);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Votre demande a bien été enregistrée. Nous vous recontacterons rapidement." });
    }

    // ── GET /api/contact/demo  (backoffice — liste rendez-vous page Contact) ──
    [HttpGet("demo")]
    [Authorize(Policy = "ContactRead")]
    public async Task<IActionResult> GetDemoContacts(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20)
    {
        var query = _db.ContactRequests
            .AsQueryable()
            .Where(c => c.Subject == DemoAppointmentSubject);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        var items = rows.Select(c => new
        {
            c.Id,
            c.FirstName,
            c.LastName,
            c.Email,
            hasWhatsApp = HasWhatsAppFromMessage(c.Message),
            c.CreatedAt,
            c.Status,
            c.InternalNote,
            c.Message,
            c.Subject
        }).ToList();

        return Ok(new { total, page, size, items });
    }

    // ── PATCH /api/contact/demo/{id}/status  (backoffice) ─────────────────────
    [HttpPatch("demo/{id:int}/status")]
    [Authorize(Policy = "ContactRead")]
    public async Task<IActionResult> UpdateDemoContactStatus(int id, [FromBody] StatusUpdateDto dto)
    {
        var item = await _db.ContactRequests.FindAsync(id);
        if (item == null) return NotFound();
        if (item.Subject != DemoAppointmentSubject)
            return BadRequest(new { message = "Cette entrée n'est pas une demande de rendez-vous." });

        item.Status = dto.Status;
        item.InternalNote = dto.InternalNote ?? item.InternalNote;
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    // ── GET /api/contact  (backoffice — Admin, RH, Commercial) ───────────────
    [HttpGet]
    [Authorize(Policy = "ContactRead")]
    public async Task<IActionResult> GetContacts(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20)
    {
        var query = _db.ContactRequests
            .AsQueryable()
            .Where(c => c.Subject != DemoAppointmentSubject);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return Ok(new { total, page, size, items });
    }

    // ── PATCH /api/contact/{id}/status ────────────────────────────────────────
    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "ContactRead")]
    public async Task<IActionResult> UpdateContactStatus(int id, [FromBody] StatusUpdateDto dto)
    {
        var item = await _db.ContactRequests.FindAsync(id);
        if (item == null) return NotFound();
        if (item.Subject == DemoAppointmentSubject)
            return BadRequest(new { message = "Utilisez l'onglet Rendez-vous pour modifier ce type de demande." });

        item.Status = dto.Status;
        item.InternalNote = dto.InternalNote ?? item.InternalNote;
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    private static bool HasWhatsAppFromMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        return message.Contains("WhatsApp : Oui", StringComparison.OrdinalIgnoreCase);
    }
}

public record StatusUpdateDto(string Status, string? InternalNote);