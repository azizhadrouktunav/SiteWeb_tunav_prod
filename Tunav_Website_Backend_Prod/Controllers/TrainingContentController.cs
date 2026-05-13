using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Controllers
{
    // ══════════════════════════════════════════════════════════════════════════
    //  /api/training-partners  — Établissements partenaires
    // ══════════════════════════════════════════════════════════════════════════
    [ApiController]
    [Route("api/training-partners")]
    public class TrainingPartnersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public TrainingPartnersController(AppDbContext db) => _db = db;

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] bool? isActive)
        {
            var q = _db.TrainingPartners.AsQueryable();
            if (isActive.HasValue) q = q.Where(p => p.IsActive == isActive.Value);
            var list = await q.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name).ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id:int}"), AllowAnonymous]
        public async Task<IActionResult> GetOne(int id)
        {
            var p = await _db.TrainingPartners.FindAsync(id);
            return p is null ? NotFound(new { message = "Partenaire introuvable." }) : Ok(p);
        }

        [HttpPost, Authorize(Policy = "EventWrite")]
        public async Task<IActionResult> Create([FromBody] TrainingPartnerRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { message = "Le nom est obligatoire." });
            if (string.IsNullOrWhiteSpace(req.Domain))
                return BadRequest(new { message = "Le domaine est obligatoire." });

            var partner = new TrainingPartner
            {
                Name = req.Name.Trim(),
                Domain = req.Domain.Trim(),
                Icon = req.Icon?.Trim(),
                ImageUrl = req.ImageUrl?.Trim(),
                IsActive = req.IsActive,
                DisplayOrder = req.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };
            _db.TrainingPartners.Add(partner);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetOne), new { id = partner.Id }, partner);
        }

        [HttpPut("{id:int}"), Authorize(Policy = "EventWrite")]
        public async Task<IActionResult> Update(int id, [FromBody] TrainingPartnerRequest req)
        {
            var p = await _db.TrainingPartners.FindAsync(id);
            if (p is null) return NotFound(new { message = "Partenaire introuvable." });

            p.Name = req.Name?.Trim() ?? p.Name;
            p.Domain = req.Domain?.Trim() ?? p.Domain;
            p.Icon = req.Icon?.Trim() ?? p.Icon;
            p.ImageUrl = req.ImageUrl is not null ? (string.IsNullOrWhiteSpace(req.ImageUrl) ? null : req.ImageUrl.Trim()) : p.ImageUrl;
            p.IsActive = req.IsActive;
            p.DisplayOrder = req.DisplayOrder;
            p.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(p);
        }

        [HttpPatch("{id:int}/toggle"), Authorize(Policy = "EventWrite")]
        public async Task<IActionResult> Toggle(int id)
        {
            var p = await _db.TrainingPartners.FindAsync(id);
            if (p is null) return NotFound(new { message = "Partenaire introuvable." });
            p.IsActive = !p.IsActive;
            p.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(p);
        }

        [HttpDelete("{id:int}"), Authorize(Policy = "EventWrite")]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _db.TrainingPartners.FindAsync(id);
            if (p is null) return NotFound(new { message = "Partenaire introuvable." });
            _db.TrainingPartners.Remove(p);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  /api/testimonials  — Témoignages employés
    // ══════════════════════════════════════════════════════════════════════════
    [ApiController]
    [Route("api/testimonials")]
    public class TestimonialsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public TestimonialsController(AppDbContext db) => _db = db;

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] bool? isActive)
        {
            var q = _db.Testimonials.AsQueryable();
            if (isActive.HasValue) q = q.Where(t => t.IsActive == isActive.Value);
            var list = await q.OrderBy(t => t.DisplayOrder).ThenBy(t => t.AuthorName).ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id:int}"), AllowAnonymous]
        public async Task<IActionResult> GetOne(int id)
        {
            var t = await _db.Testimonials.FindAsync(id);
            return t is null ? NotFound(new { message = "Témoignage introuvable." }) : Ok(t);
        }

        [HttpPost, Authorize(Policy = "EventWrite")]
        public async Task<IActionResult> Create([FromBody] TestimonialRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.AuthorName))
                return BadRequest(new { message = "Le nom de l'auteur est obligatoire." });
            if (string.IsNullOrWhiteSpace(req.Content))
                return BadRequest(new { message = "Le contenu est obligatoire." });

            var t = new Testimonial
            {
                AuthorName = req.AuthorName.Trim(),
                AuthorRole = req.AuthorRole?.Trim() ?? "",
                Company = req.Company?.Trim(),
                Avatar = req.Avatar?.Trim(),
                Content = req.Content.Trim(),
                Rating = Math.Clamp(req.Rating, 1, 5),
                IsActive = req.IsActive,
                DisplayOrder = req.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };
            _db.Testimonials.Add(t);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetOne), new { id = t.Id }, t);
        }

        [HttpPut("{id:int}"), Authorize(Policy = "EventWrite")]
        public async Task<IActionResult> Update(int id, [FromBody] TestimonialRequest req)
        {
            var t = await _db.Testimonials.FindAsync(id);
            if (t is null) return NotFound(new { message = "Témoignage introuvable." });

            t.AuthorName = req.AuthorName?.Trim() ?? t.AuthorName;
            t.AuthorRole = req.AuthorRole?.Trim() ?? t.AuthorRole;
            t.Company = req.Company?.Trim() ?? t.Company;
            t.Avatar = req.Avatar?.Trim() ?? t.Avatar;
            t.Content = req.Content?.Trim() ?? t.Content;
            t.Rating = Math.Clamp(req.Rating, 1, 5);
            t.IsActive = req.IsActive;
            t.DisplayOrder = req.DisplayOrder;
            t.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(t);
        }

        [HttpPatch("{id:int}/toggle"), Authorize(Policy = "EventWrite")]
        public async Task<IActionResult> Toggle(int id)
        {
            var t = await _db.Testimonials.FindAsync(id);
            if (t is null) return NotFound(new { message = "Témoignage introuvable." });
            t.IsActive = !t.IsActive;
            t.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(t);
        }

        [HttpDelete("{id:int}"), Authorize(Policy = "EventWrite")]
        public async Task<IActionResult> Delete(int id)
        {
            var t = await _db.Testimonials.FindAsync(id);
            if (t is null) return NotFound(new { message = "Témoignage introuvable." });
            _db.Testimonials.Remove(t);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }

    // ── Request records ───────────────────────────────────────────────────────
    public record TrainingPartnerRequest(
        string Name, string Domain, string? Icon,
        string? ImageUrl,
        bool IsActive = true, int DisplayOrder = 0);

    public record TestimonialRequest(
        string AuthorName, string? AuthorRole, string? Company,
        string? Avatar, string Content,
        int Rating = 5, bool IsActive = true, int DisplayOrder = 0);
}