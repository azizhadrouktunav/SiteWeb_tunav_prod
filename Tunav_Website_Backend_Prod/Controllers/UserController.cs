using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Controllers;

public class UserController : Controller
{
    private readonly AppDbContext _context;

    public UserController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _context.Users
            .Include(u => u.Role)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return View(list);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        await LoadRolesAsync();

        if (id is null)
            return View(new User { IsActive = true });

        var item = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (item is null) return NotFound();

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(User model)
    {
        if (!ModelState.IsValid)
        {
            await LoadRolesAsync();
            return View(model);
        }

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == model.Email && u.Id != model.Id);

        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email),
                $"Un utilisateur avec l'email '{model.Email}' existe déjà.");
            await LoadRolesAsync();
            return View(model);
        }

        if (model.Id == 0)
        {
            model.CreatedAt = DateTime.UtcNow;
            model.IsActive = true;
            model.PasswordHash = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(model.PasswordHash));
            _context.Users.Add(model);
        }
        else
        {
            var existing = await _context.Users.FindAsync(model.Id);
            if (existing is null) return NotFound();

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
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// US-A04 — Active ou désactive un compte utilisateur
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var existing = await _context.Users.FindAsync(id);
        if (existing is not null)
        {
            existing.IsActive = !existing.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Message"] = existing.IsActive
                ? $"✅ Le compte de {existing.FirstName} {existing.LastName} a été activé."
                : $"⛔ Le compte de {existing.FirstName} {existing.LastName} a été désactivé.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _context.Users.FindAsync(id);
        if (existing is not null)
        {
            _context.Users.Remove(existing);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadRolesAsync()
    {
        var roles = await _context.Roles
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();

        ViewBag.Roles = new SelectList(roles, "Id", "Name");
    }
}