using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Services;

public static class PackCatalogMaintenance
{
    public static async Task EnsureMetadataAsync(AppDbContext db)
    {
        var solutions = await db.Solutions.ToListAsync();
        var changed = false;

        foreach (var solution in solutions)
        {
            if (string.IsNullOrWhiteSpace(solution.Slug))
            {
                solution.Slug = await ResolveUniqueSlugAsync(db, solution.Title, solution.Id);
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(solution.PackIconKey))
            {
                solution.PackIconKey = PackPresentationCatalog.DefaultSolutionIconKey;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(solution.PackThemeKey))
            {
                solution.PackThemeKey = PackPresentationCatalog.DefaultSolutionThemeKey;
                changed = true;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }
    }

    private static async Task<string> ResolveUniqueSlugAsync(AppDbContext db, string title, int excludeId)
    {
        var baseSlug = SlugHelper.GenerateSlug(title);
        var candidate = baseSlug;
        var suffix = 2;

        while (await db.Solutions.AnyAsync(solution => solution.Id != excludeId && solution.Slug == candidate))
        {
            var suffixText = "-" + suffix++;
            var maxBaseLength = Math.Max(1, 120 - suffixText.Length);
            candidate = baseSlug.Length > maxBaseLength
                ? baseSlug[..maxBaseLength].Trim('-') + suffixText
                : baseSlug + suffixText;
        }

        return candidate;
    }
}
