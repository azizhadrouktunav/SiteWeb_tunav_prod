using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Controllers.Api;

[ApiController]
[Route("api/industry-sectors")]
public class IndustrySectorController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public IndustrySectorController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ── GET api/industry-sectors  (public — front Angular) ───────────────────
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.IndustrySectors
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new {
                s.Id,
                s.Title,
                s.Description,
                s.ImageUrl,
                s.DisplayOrder
            })
            .ToListAsync();
        return Ok(list);
    }

    // ── GET api/industry-sectors/all  (backoffice — toutes, actives + inactives) ─
    [HttpGet("all")]
    [Authorize(Policy = "SectorWrite")]
    public async Task<IActionResult> GetAllAdmin()
    {
        var list = await _db.IndustrySectors
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();
        return Ok(list);
    }

    // ── POST api/industry-sectors ─────────────────────────────────────────────
    [HttpPost]
    [Authorize(Policy = "SectorWrite")]
    public async Task<IActionResult> Create([FromForm] IndustrySectorDto dto)
    {
        var sector = new IndustrySector
        {
            Title = dto.Title,
            Description = dto.Description,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            ImageUrl = string.Empty
        };

        if (dto.Image != null)
            sector.ImageUrl = await SaveImage(dto.Image);

        _db.IndustrySectors.Add(sector);
        await _db.SaveChangesAsync();
        return Ok(sector);
    }

    // ── PUT api/industry-sectors/{id} ─────────────────────────────────────────
    [HttpPut("{id}")]
    [Authorize(Policy = "SectorWrite")]
    public async Task<IActionResult> Update(int id, [FromForm] IndustrySectorDto dto)
    {
        var sector = await _db.IndustrySectors.FindAsync(id);
        if (sector == null) return NotFound();

        sector.Title = dto.Title;
        sector.Description = dto.Description;
        sector.DisplayOrder = dto.DisplayOrder;
        sector.IsActive = dto.IsActive;

        if (dto.Image != null)
            sector.ImageUrl = await SaveImage(dto.Image);
        else if (!string.IsNullOrEmpty(dto.ImageUrl))
            sector.ImageUrl = dto.ImageUrl;

        await _db.SaveChangesAsync();
        return Ok(sector);
    }

    // ── PATCH api/industry-sectors/{id}/toggle ────────────────────────────────
    [HttpPatch("{id}/toggle")]
    [Authorize(Policy = "SectorWrite")]
    public async Task<IActionResult> Toggle(int id)
    {
        var sector = await _db.IndustrySectors.FindAsync(id);
        if (sector == null) return NotFound();
        sector.IsActive = !sector.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { sector.Id, sector.IsActive });
    }

    // ── DELETE api/industry-sectors/{id} ─────────────────────────────────────
    [HttpDelete("{id}")]
    [Authorize(Policy = "SectorWrite")]
    public async Task<IActionResult> Delete(int id)
    {
        var sector = await _db.IndustrySectors.FindAsync(id);
        if (sector == null) return NotFound();
        _db.IndustrySectors.Remove(sector);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private async Task<string> SaveImage(IFormFile file)
    {
        var dir = Path.Combine(_env.ContentRootPath, "Uploads", "sectors");
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        using var stream = System.IO.File.Create(Path.Combine(dir, name));
        await file.CopyToAsync(stream);
        return $"/uploads/sectors/{name}";
    }
}

public class IndustrySectorDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public IFormFile? Image { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}