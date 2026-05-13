using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Controllers;

public class RoleController : Controller
{
    private readonly AppDbContext _context;

    public RoleController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _context.Roles
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(list);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return View(new Role { IsActive = true });
        }

        var item = await _context.Roles.FindAsync(id.Value);
        if (item is null) return NotFound();

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Role model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Vérification doublon sur Name
        var nameExists = await _context.Roles
            .AnyAsync(r => r.Name == model.Name && r.Id != model.Id);

        if (nameExists)
        {
            ModelState.AddModelError(nameof(model.Name), $"Un rôle avec le nom '{model.Name}' existe déjà.");
            return View(model);
        }

        if (model.Id == 0)
        {
            model.CreatedAt = DateTime.UtcNow;
            model.IsActive = true;
            _context.Roles.Add(model);
        }
        else
        {
            var existing = await _context.Roles.FindAsync(model.Id);
            if (existing is null) return NotFound();

            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.IsActive = model.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _context.Roles.FindAsync(id);
        if (existing is not null)
        {
            _context.Roles.Remove(existing);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}