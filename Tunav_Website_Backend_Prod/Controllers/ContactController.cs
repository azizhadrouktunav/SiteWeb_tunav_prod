using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
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

    // ── GET /api/contact  (backoffice — Admin, RH, Commercial) ───────────────
    [HttpGet]
    [Authorize(Policy = "ContactRead")]
    public async Task<IActionResult> GetContacts(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20)
    {
        var query = _db.ContactRequests.AsQueryable();
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
        item.Status = dto.Status;
        item.InternalNote = dto.InternalNote ?? item.InternalNote;
        await _db.SaveChangesAsync();
        return Ok(item);
    }
}

public record StatusUpdateDto(string Status, string? InternalNote);