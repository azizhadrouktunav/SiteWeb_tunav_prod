using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Services;

public record PackDto(
    int Id,
    int SolutionId,
    string SolutionTitle,
    string SolutionSlug,
    string Name,
    string Description,
    List<string> Features,
    string ThemeKey,
    int DisplayOrder,
    bool IsPopular,
    string? VideoUrl,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record PackCatalogSolutionDto(
    int SolutionId,
    string SolutionTitle,
    string SolutionSlug,
    string PackIconKey,
    string PackThemeKey,
    List<PackDto> Packs);

public record GeneralSolutionOptionDto(
    int Id,
    string Title,
    string Slug,
    bool IsActive);

public record CreatePackDto(
    int SolutionId,
    string Name,
    string Description,
    List<string>? Features,
    string? ThemeKey,
    int DisplayOrder,
    bool IsPopular,
    string? VideoUrl,
    bool IsActive = true);

public record UpdatePackDto(
    int? SolutionId,
    string? Name,
    string? Description,
    List<string>? Features,
    string? ThemeKey,
    int? DisplayOrder,
    bool? IsPopular,
    string? VideoUrl,
    bool? IsActive);

public interface IPackService
{
    Task<List<PackCatalogSolutionDto>> GetCatalogAsync();
    Task<List<PackDto>> GetAllAsync(int? solutionId = null, bool? isActive = null, string? search = null);
    Task<PackDto?> GetByIdAsync(int id);
    Task<List<GeneralSolutionOptionDto>> GetGeneralSolutionsAsync();
    Task<PackDto> CreateAsync(CreatePackDto dto);
    Task<PackDto?> UpdateAsync(int id, UpdatePackDto dto);
    Task<PackDto?> ToggleAsync(int id);
    Task<bool> DeleteAsync(int id);
}

public class PackService : IPackService
{
    private readonly AppDbContext _db;

    public PackService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<PackCatalogSolutionDto>> GetCatalogAsync()
    {
        var solutions = await _db.Solutions
            .AsNoTracking()
            .Where(solution => solution.Type == SolutionType.General && solution.IsActive)
            .OrderBy(solution => solution.Title)
            .ToListAsync();

        var packs = await _db.Packs
            .AsNoTracking()
            .Where(pack => pack.IsActive)
            .OrderBy(pack => pack.DisplayOrder)
            .ThenBy(pack => pack.Name)
            .ToListAsync();

        var packsBySolutionId = packs
            .GroupBy(pack => pack.SolutionId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return solutions
            .Where(solution => packsBySolutionId.ContainsKey(solution.Id))
            .OrderBy(solution => solution.Title, StringComparer.OrdinalIgnoreCase)
            .Select(solution => new PackCatalogSolutionDto(
                solution.Id,
                solution.Title,
                solution.Slug,
                string.IsNullOrWhiteSpace(solution.PackIconKey)
                    ? PackPresentationCatalog.DefaultSolutionIconKey
                    : solution.PackIconKey,
                string.IsNullOrWhiteSpace(solution.PackThemeKey)
                    ? PackPresentationCatalog.DefaultSolutionThemeKey
                    : solution.PackThemeKey,
                packsBySolutionId[solution.Id]
                    .Select(pack => ToDto(pack, solution))
                    .ToList()))
            .ToList();
    }

    public async Task<List<PackDto>> GetAllAsync(int? solutionId = null, bool? isActive = null, string? search = null)
    {
        var query = _db.Packs
            .AsNoTracking()
            .Include(pack => pack.Solution)
            .AsQueryable();

        if (solutionId.HasValue && solutionId.Value > 0)
        {
            query = query.Where(pack => pack.SolutionId == solutionId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(pack => pack.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(pack =>
                EF.Functions.Like(pack.Name, $"%{term}%") ||
                EF.Functions.Like(pack.Description, $"%{term}%") ||
                (pack.Solution != null && EF.Functions.Like(pack.Solution.Title, $"%{term}%")));
        }

        var packs = await query
            .OrderBy(pack => pack.Solution!.Title)
            .ThenBy(pack => pack.DisplayOrder)
            .ThenBy(pack => pack.Name)
            .ToListAsync();

        return packs
            .Where(pack => pack.Solution != null)
            .Select(pack => ToDto(pack, pack.Solution!))
            .ToList();
    }

    public async Task<PackDto?> GetByIdAsync(int id)
    {
        var pack = await _db.Packs
            .AsNoTracking()
            .Include(item => item.Solution)
            .FirstOrDefaultAsync(item => item.Id == id);

        return pack?.Solution == null ? null : ToDto(pack, pack.Solution);
    }

    public async Task<List<GeneralSolutionOptionDto>> GetGeneralSolutionsAsync()
    {
        var solutions = await _db.Solutions
            .AsNoTracking()
            .Where(solution => solution.Type == SolutionType.General)
            .OrderBy(solution => solution.Title)
            .ToListAsync();

        return solutions
            .OrderBy(solution => solution.Title, StringComparer.OrdinalIgnoreCase)
            .Select(solution => new GeneralSolutionOptionDto(
                solution.Id,
                solution.Title,
                solution.Slug,
                solution.IsActive))
            .ToList();
    }

    public async Task<PackDto> CreateAsync(CreatePackDto dto)
    {
        var owner = await GetGeneralSolutionAsync(dto.SolutionId);
        var name = NormalizeName(dto.Name);
        await EnsurePackNameIsUniqueAsync(owner.Id, name, null);

        var features = NormalizeFeatures(dto.Features);
        var featuresJson = SerializeFeatures(features);

        var pack = new Pack
        {
            SolutionId = owner.Id,
            Name = name,
            Description = NormalizeDescription(dto.Description),
            FeaturesJson = featuresJson,
            ThemeKey = PackPresentationCatalog.NormalizePackThemeKey(dto.ThemeKey),
            DisplayOrder = NormalizeDisplayOrder(dto.DisplayOrder),
            IsPopular = dto.IsPopular,
            VideoUrl = NormalizeVideoUrl(dto.VideoUrl),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Packs.Add(pack);
        await _db.SaveChangesAsync();

        return ToDto(pack, owner);
    }

    public async Task<PackDto?> UpdateAsync(int id, UpdatePackDto dto)
    {
        var pack = await _db.Packs
            .Include(item => item.Solution)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (pack == null)
        {
            return null;
        }

        Solution owner = pack.Solution ?? await GetGeneralSolutionAsync(pack.SolutionId);
        var solutionChanged = false;
        var nameChanged = false;

        if (dto.SolutionId.HasValue && dto.SolutionId.Value <= 0)
        {
            throw new InvalidOperationException("Veuillez choisir une solution generale.");
        }

        if (dto.SolutionId.HasValue && dto.SolutionId.Value != pack.SolutionId)
        {
            owner = await GetGeneralSolutionAsync(dto.SolutionId.Value);
            pack.SolutionId = owner.Id;
            solutionChanged = true;
        }

        if (dto.Name != null)
        {
            var name = NormalizeName(dto.Name);
            nameChanged = !string.Equals(pack.Name, name, StringComparison.OrdinalIgnoreCase);
            pack.Name = name;
        }

        if (solutionChanged || nameChanged)
        {
            await EnsurePackNameIsUniqueAsync(owner.Id, pack.Name, pack.Id);
        }

        if (dto.Description != null)
        {
            pack.Description = NormalizeDescription(dto.Description);
        }

        if (dto.Features != null)
        {
            pack.FeaturesJson = SerializeFeatures(NormalizeFeatures(dto.Features));
        }

        if (dto.ThemeKey != null)
        {
            pack.ThemeKey = PackPresentationCatalog.NormalizePackThemeKey(dto.ThemeKey);
        }

        if (dto.DisplayOrder.HasValue)
        {
            pack.DisplayOrder = NormalizeDisplayOrder(dto.DisplayOrder.Value);
        }

        if (dto.IsPopular.HasValue)
        {
            pack.IsPopular = dto.IsPopular.Value;
        }

        if (dto.VideoUrl != null)
        {
            pack.VideoUrl = NormalizeVideoUrl(dto.VideoUrl);
        }

        if (dto.IsActive.HasValue)
        {
            pack.IsActive = dto.IsActive.Value;
        }

        pack.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return ToDto(pack, owner);
    }

    public async Task<PackDto?> ToggleAsync(int id)
    {
        var pack = await _db.Packs
            .Include(item => item.Solution)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (pack == null || pack.Solution == null)
        {
            return null;
        }

        pack.IsActive = !pack.IsActive;
        pack.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ToDto(pack, pack.Solution);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var pack = await _db.Packs.FirstOrDefaultAsync(item => item.Id == id);
        if (pack == null)
        {
            return false;
        }

        _db.Packs.Remove(pack);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<Solution> GetGeneralSolutionAsync(int solutionId)
    {
        var solution = await _db.Solutions
            .FirstOrDefaultAsync(item => item.Id == solutionId);

        if (solution == null)
        {
            throw new InvalidOperationException("La solution sélectionnée est introuvable.");
        }

        if (solution.Type != SolutionType.General)
        {
            throw new InvalidOperationException("Seules les solutions générales peuvent posséder des packs.");
        }

        return solution;
    }

    private async Task EnsurePackNameIsUniqueAsync(int solutionId, string name, int? excludeId)
    {
        var normalizedName = name.ToLowerInvariant();
        var duplicateExists = await _db.Packs.AnyAsync(pack =>
            pack.SolutionId == solutionId &&
            pack.Name.ToLower() == normalizedName &&
            (!excludeId.HasValue || pack.Id != excludeId.Value));

        if (duplicateExists)
        {
            throw new InvalidOperationException("Un pack avec ce nom existe deja pour cette solution.");
        }
    }

    private static string NormalizeName(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length < 2 || normalized.Length > 180)
        {
            throw new InvalidOperationException("Le nom du pack doit contenir entre 2 et 180 caractères.");
        }

        return normalized;
    }

    private static string NormalizeDescription(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length < 10 || normalized.Length > 1200)
        {
            throw new InvalidOperationException("La description du pack doit contenir entre 10 et 1200 caractères.");
        }

        return normalized;
    }

    private static List<string> NormalizeFeatures(IEnumerable<string>? values)
    {
        var features = (values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (features.Count == 0)
        {
            throw new InvalidOperationException("Un pack doit contenir au moins une fonctionnalité.");
        }

        if (features.Count > 80)
        {
            throw new InvalidOperationException("Un pack ne peut pas contenir plus de 80 fonctionnalités.");
        }

        if (features.Any(feature => feature.Length > 200))
        {
            throw new InvalidOperationException("Une fonctionnalité ne peut pas dépasser 200 caractères.");
        }

        return features;
    }

    private static int NormalizeDisplayOrder(int value)
    {
        if (value < 0 || value > 10000)
        {
            throw new InvalidOperationException("L'ordre d'affichage doit etre compris entre 0 et 10000.");
        }

        return value;
    }

    private static string? NormalizeVideoUrl(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return null;
        }

        if (normalized.Length > 500)
        {
            throw new InvalidOperationException("L'URL vidéo ne peut pas dépasser 500 caractères.");
        }

        var isAbsolute = Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        if (!isAbsolute)
        {
            throw new InvalidOperationException("L'URL vidéo doit être une URL HTTP/HTTPS valide.");
        }

        return normalized;
    }

    private static string SerializeFeatures(List<string> features)
    {
        var json = JsonSerializer.Serialize(features);
        if (json.Length > 12000)
        {
            throw new InvalidOperationException("La liste des fonctionnalites est trop longue.");
        }

        return json;
    }

    private static List<string> DeserializeFeatures(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static PackDto ToDto(Pack pack, Solution solution)
    {
        return new PackDto(
            pack.Id,
            solution.Id,
            solution.Title,
            solution.Slug,
            pack.Name,
            pack.Description,
            DeserializeFeatures(pack.FeaturesJson),
            pack.ThemeKey,
            pack.DisplayOrder,
            pack.IsPopular,
            pack.VideoUrl,
            pack.IsActive,
            pack.CreatedAt,
            pack.UpdatedAt);
    }
}
