using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Controllers;

// ════════════════════════════════════════════════════════════════════════════
// CLIENTS API
// ════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ClientsController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // GET /api/clients  (public — frontoffice)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive)
    {
        var q = _db.Clients.AsQueryable();
        if (isActive.HasValue) q = q.Where(c => c.IsActive == isActive.Value);
        var list = await q.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync();
        return Ok(list);
    }

    // GET /api/clients/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await _db.Clients.FindAsync(id);
        return c == null ? NotFound() : Ok(c);
    }

    // POST /api/clients  (Admin only)
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] Client dto)
    {
        dto.CreatedAt = DateTime.UtcNow;
        _db.Clients.Add(dto);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    // PUT /api/clients/{id}
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, [FromBody] Client dto)
    {
        var existing = await _db.Clients.FindAsync(id);
        if (existing == null) return NotFound();
        existing.Name = dto.Name;
        existing.LogoUrl = dto.LogoUrl;
        existing.Website = dto.Website;
        existing.Sector = dto.Sector;
        existing.Description = dto.Description;
        existing.DisplayOrder = dto.DisplayOrder;
        existing.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    // PATCH /api/clients/{id}/toggle
    [HttpPatch("{id}/toggle")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Toggle(int id)
    {
        var c = await _db.Clients.FindAsync(id);
        if (c == null) return NotFound();
        c.IsActive = !c.IsActive;
        await _db.SaveChangesAsync();
        return Ok(c);
    }

    // DELETE /api/clients/{id}
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Clients.FindAsync(id);
        if (c == null) return NotFound();
        _db.Clients.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/clients/{id}/logo  — upload logo
    [HttpPost("{id}/logo")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UploadLogo(int id, IFormFile file)
    {
        var c = await _db.Clients.FindAsync(id);
        if (c == null) return NotFound();
        if (file == null || file.Length == 0) return BadRequest("Fichier requis.");

        var uploadsDir = Path.Combine(_env.ContentRootPath, "Uploads", "clients");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"client_{id}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsDir, fileName);
        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream);
        c.LogoUrl = $"/uploads/clients/{fileName}";
        await _db.SaveChangesAsync();
        return Ok(new { logoUrl = c.LogoUrl });
    }
}

// ════════════════════════════════════════════════════════════════════════════
// PARTNERS API
// ════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/partners")]
public class PartnersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public PartnersController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // GET /api/partners  (public)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive)
    {
        var q = _db.Partners.AsQueryable();
        if (isActive.HasValue) q = q.Where(p => p.IsActive == isActive.Value);
        var list = await q.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name).ToListAsync();
        return Ok(list);
    }

    // GET /api/partners/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _db.Partners.FindAsync(id);
        return p == null ? NotFound() : Ok(p);
    }

    // POST /api/partners
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] Partner dto)
    {
        dto.CreatedAt = DateTime.UtcNow;
        _db.Partners.Add(dto);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    // PUT /api/partners/{id}
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, [FromBody] Partner dto)
    {
        var existing = await _db.Partners.FindAsync(id);
        if (existing == null) return NotFound();
        existing.Name = dto.Name;
        existing.LogoUrl = dto.LogoUrl;
        existing.Website = dto.Website;
        existing.Country = dto.Country;
        existing.ContactPerson = dto.ContactPerson;
        existing.ContactEmail = dto.ContactEmail;
        existing.Description = dto.Description;
        existing.PartnerType = dto.PartnerType;
        existing.DisplayOrder = dto.DisplayOrder;
        existing.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    // PATCH /api/partners/{id}/toggle
    [HttpPatch("{id}/toggle")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Toggle(int id)
    {
        var p = await _db.Partners.FindAsync(id);
        if (p == null) return NotFound();
        p.IsActive = !p.IsActive;
        await _db.SaveChangesAsync();
        return Ok(p);
    }

    // DELETE /api/partners/{id}
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Partners.FindAsync(id);
        if (p == null) return NotFound();
        _db.Partners.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/partners/{id}/logo
    [HttpPost("{id}/logo")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UploadLogo(int id, IFormFile file)
    {
        var p = await _db.Partners.FindAsync(id);
        if (p == null) return NotFound();
        if (file == null || file.Length == 0) return BadRequest("Fichier requis.");

        var uploadsDir = Path.Combine(_env.ContentRootPath, "Uploads", "partners");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"partner_{id}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsDir, fileName);
        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream);
        p.LogoUrl = $"/uploads/partners/{fileName}";
        await _db.SaveChangesAsync();
        return Ok(new { logoUrl = p.LogoUrl });
    }
}