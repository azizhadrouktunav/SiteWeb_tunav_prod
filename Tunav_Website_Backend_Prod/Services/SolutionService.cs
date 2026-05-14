using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using tunav_backend.DTOs;
using tunav_backend.Models;

namespace tunav_backend.Services;

/// <summary>
/// Solution service implementation
/// Handles all solution-related business logic
/// </summary>
public class SolutionService : ISolutionService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SolutionService> _logger;

    /// <summary>Aligné sur UploadController et le middleware fichiers statiques (/uploads → ContentRoot/Uploads).</summary>
    private string SolutionUploadPath => Path.Combine(
        _environment.ContentRootPath,
        "Uploads",
        "solutions");

    public SolutionService(
        AppDbContext context,
        IWebHostEnvironment environment,
        ILogger<SolutionService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> GetAvailableSectorsAsync()
    {
        var sectorSources = await _context.Solutions
            .Select(s => new { s.SectorName, s.SectorsJson })
            .ToListAsync();

        var sectors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sectorSources)
        {
            AddSectorValue(sectors, source.SectorName);

            foreach (var item in DeserializeStringList(source.SectorsJson))
            {
                AddSectorValue(sectors, item);
            }
        }

        return sectors
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IEnumerable<SolutionDto>> GetAllSolutionsAsync(
        bool? isActiveOnly = null,
        bool? isActive = null,
        string? type = null,
        string? sector = null,
        string? search = null)
    {
        var query = _context.Solutions
            .Include(s => s.CreatedByUser)
            .Include(s => s.BaseSolution)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }
        else if (isActiveOnly.HasValue && isActiveOnly.Value)
        {
            query = query.Where(s => s.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var validType = ConvertStringToSolutionType(type);
            query = query.Where(s => s.Type == validType);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                EF.Functions.Like(s.Title, $"%{term}%") ||
                EF.Functions.Like(s.Description, $"%{term}%") ||
                EF.Functions.Like(s.Slug, $"%{term}%"));
        }

        var solutions = await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var activePackSolutionIds = await GetActivePackSolutionIdsAsync();
        var result = solutions.Select(solution => MapToDto(solution, activePackSolutionIds)).ToList();

        if (!string.IsNullOrWhiteSpace(sector))
        {
            var normalizedSector = NormalizeRequiredSectorName(sector);
            result = result
                .Where(solution =>
                    string.Equals(solution.SectorName, normalizedSector, StringComparison.OrdinalIgnoreCase) ||
                    solution.Sectors.Any(item => string.Equals(item, normalizedSector, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return result;
    }

    public async Task<SolutionDto?> GetSolutionByIdAsync(int id)
    {
        var solution = await _context.Solutions
            .Include(s => s.CreatedByUser)
            .Include(s => s.BaseSolution)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solution == null)
        {
            return null;
        }

        var activePackSolutionIds = await GetActivePackSolutionIdsAsync();
        return MapToDto(solution, activePackSolutionIds);
    }

    public async Task<IEnumerable<SolutionDto>> GetSolutionsByTypeAsync(string type)
    {
        var solutionType = ConvertStringToSolutionType(type);

        var solutions = await _context.Solutions
            .Include(s => s.CreatedByUser)
            .Include(s => s.BaseSolution)
            .Where(s => s.Type == solutionType && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var activePackSolutionIds = await GetActivePackSolutionIdsAsync();
        return solutions.Select(solution => MapToDto(solution, activePackSolutionIds)).ToList();
    }

    public async Task<SolutionDto> CreateSolutionAsync(CreateSolutionDto dto, int userId)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            throw new InvalidOperationException($"User with ID {userId} does not exist.");
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new InvalidOperationException("Title is required and cannot be empty.");
        }

        var solutionType = ConvertStringToSolutionType(dto.Type);
        var normalizedSectors = NormalizeSectorList(dto.Sectors);
        var normalizedTopClients = NormalizeStringList(dto.TopClients, 20, 120, "TopClients");
        var normalizedFunctionalities = NormalizeStringList(dto.Functionalities, 40, 160, "Functionalities");
        var normalizedAdvantages = NormalizeStringList(dto.Advantages, 40, 180, "Advantages");
        var normalizedUseCases = NormalizeUseCases(dto.UseCases);
        var resolvedSectorName = ResolveSectorName(solutionType, dto.SectorName, normalizedSectors);
        var slug = await ResolveUniqueSlugAsync(dto.Slug, dto.Title, null);
        var baseSolutionId = await ResolveBaseSolutionIdAsync(solutionType, dto.BaseSolutionId, null);

        if (resolvedSectorName != null &&
            !normalizedSectors.Contains(resolvedSectorName, StringComparer.OrdinalIgnoreCase))
        {
            normalizedSectors.Insert(0, resolvedSectorName);
        }

        var solution = new Solution
        {
            Title = dto.Title.Trim(),
            Slug = slug,
            Description = dto.Description?.Trim() ?? string.Empty,
            Type = solutionType,
            SectorName = resolvedSectorName,
            BaseSolutionId = baseSolutionId,
            PackIconKey = PackPresentationCatalog.NormalizeSolutionIconKey(dto.PackIconKey),
            PackThemeKey = PackPresentationCatalog.NormalizeSolutionThemeKey(dto.PackThemeKey),
            CoverImageUrl = NormalizeCoverImageUrl(dto.CoverImageUrl),
            YoutubeUrl = NormalizeYoutubeUrl(dto.YoutubeUrl),
            SectorsJson = SerializeStringList(normalizedSectors),
            TopClientsJson = SerializeStringList(normalizedTopClients),
            FunctionalitiesJson = SerializeStringList(normalizedFunctionalities),
            AdvantagesJson = SerializeStringList(normalizedAdvantages),
            UseCasesJson = SerializeUseCases(normalizedUseCases),
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Solutions.Add(solution);
        await _context.SaveChangesAsync();

        await _context.Entry(solution).Reference(s => s.CreatedByUser).LoadAsync();
        await _context.Entry(solution).Reference(s => s.BaseSolution).LoadAsync();

        var activePackSolutionIds = await GetActivePackSolutionIdsAsync();
        return MapToDto(solution, activePackSolutionIds);
    }

    public async Task<SolutionDto> UpdateSolutionAsync(int id, UpdateSolutionDto dto)
    {
        var solution = await _context.Solutions
            .Include(s => s.CreatedByUser)
            .Include(s => s.BaseSolution)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solution == null)
        {
            throw new KeyNotFoundException($"Solution with ID {id} does not exist.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Title))
        {
            if (dto.Title.Length < 3 || dto.Title.Length > 255)
            {
                throw new InvalidOperationException("Title must be between 3 and 255 characters.");
            }

            solution.Title = dto.Title.Trim();
        }

        if (dto.Description != null)
        {
            solution.Description = dto.Description.Trim();
        }

        var currentSectors = DeserializeStringList(solution.SectorsJson);
        if (dto.Sectors != null)
        {
            currentSectors = NormalizeSectorList(dto.Sectors);
            solution.SectorsJson = SerializeStringList(currentSectors);
        }

        if (dto.TopClients != null)
        {
            var normalizedTopClients = NormalizeStringList(dto.TopClients, 20, 120, "TopClients");
            solution.TopClientsJson = SerializeStringList(normalizedTopClients);
        }

        if (dto.Functionalities != null)
        {
            var normalizedFunctionalities = NormalizeStringList(dto.Functionalities, 40, 160, "Functionalities");
            solution.FunctionalitiesJson = SerializeStringList(normalizedFunctionalities);
        }

        if (dto.Advantages != null)
        {
            var normalizedAdvantages = NormalizeStringList(dto.Advantages, 40, 180, "Advantages");
            solution.AdvantagesJson = SerializeStringList(normalizedAdvantages);
        }

        if (dto.UseCases != null)
        {
            var normalizedUseCases = NormalizeUseCases(dto.UseCases);
            solution.UseCasesJson = SerializeUseCases(normalizedUseCases);
        }

        if (dto.CoverImageUrl != null)
        {
            solution.CoverImageUrl = NormalizeCoverImageUrl(dto.CoverImageUrl);
        }

        if (dto.YoutubeUrl != null)
        {
            solution.YoutubeUrl = NormalizeYoutubeUrl(dto.YoutubeUrl);
        }

        if (!string.IsNullOrWhiteSpace(dto.Type))
        {
            solution.Type = ConvertStringToSolutionType(dto.Type);
        }

        if (dto.SectorName != null)
        {
            solution.SectorName = NormalizeOptionalSectorName(dto.SectorName);
        }

        if (dto.Slug != null)
        {
            solution.Slug = await ResolveUniqueSlugAsync(dto.Slug, solution.Title, solution.Id);
        }
        else if (string.IsNullOrWhiteSpace(solution.Slug))
        {
            solution.Slug = await ResolveUniqueSlugAsync(null, solution.Title, solution.Id);
        }

        if (dto.PackIconKey != null)
        {
            solution.PackIconKey = PackPresentationCatalog.NormalizeSolutionIconKey(dto.PackIconKey);
        }
        else if (string.IsNullOrWhiteSpace(solution.PackIconKey))
        {
            solution.PackIconKey = PackPresentationCatalog.DefaultSolutionIconKey;
        }

        if (dto.PackThemeKey != null)
        {
            solution.PackThemeKey = PackPresentationCatalog.NormalizeSolutionThemeKey(dto.PackThemeKey);
        }
        else if (string.IsNullOrWhiteSpace(solution.PackThemeKey))
        {
            solution.PackThemeKey = PackPresentationCatalog.DefaultSolutionThemeKey;
        }

        if (solution.Type == SolutionType.General)
        {
            solution.SectorName = null;
            solution.BaseSolutionId = null;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(solution.SectorName))
            {
                if (currentSectors.Any())
                {
                    solution.SectorName = currentSectors[0];
                }
                else
                {
                    throw new InvalidOperationException("At least one sector is required for Sectorial solutions.");
                }
            }

            if (!currentSectors.Contains(solution.SectorName, StringComparer.OrdinalIgnoreCase))
            {
                currentSectors.Insert(0, solution.SectorName);
                solution.SectorsJson = SerializeStringList(currentSectors);
            }

            var shouldClearBaseSolution = dto.ClearBaseSolution.GetValueOrDefault(false);
            if (shouldClearBaseSolution)
            {
                solution.BaseSolutionId = null;
            }
            else if (dto.BaseSolutionId.HasValue)
            {
                solution.BaseSolutionId = await ResolveBaseSolutionIdAsync(solution.Type, dto.BaseSolutionId, solution.Id);
            }
        }

        if (dto.IsActive.HasValue)
        {
            solution.IsActive = dto.IsActive.Value;
        }

        solution.UpdatedAt = DateTime.UtcNow;

        _context.Solutions.Update(solution);
        await _context.SaveChangesAsync();

        await _context.Entry(solution).Reference(s => s.BaseSolution).LoadAsync();

        var activePackSolutionIds = await GetActivePackSolutionIdsAsync();
        return MapToDto(solution, activePackSolutionIds);
    }

    public async Task<bool> DeleteSolutionAsync(int id)
    {
        var solution = await _context.Solutions
            .Include(s => s.DerivedSolutions)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solution == null)
        {
            throw new KeyNotFoundException($"Solution with ID {id} does not exist.");
        }

        // FK self-reference en Restrict : impossible de supprimer une solution générale
        // tant que des sectorielles pointent vers elle. On détache plutôt que d'échouer en 500.
        foreach (var derived in solution.DerivedSolutions.ToList())
        {
            derived.BaseSolutionId = null;
        }

        var coverImageUrl = solution.CoverImageUrl;

        _context.Solutions.Remove(solution);
        await _context.SaveChangesAsync();

        DeleteLocalSolutionImage(coverImageUrl);

        return true;
    }

    private async Task<HashSet<int>> GetActivePackSolutionIdsAsync()
    {
        var solutionIds = await _context.Solutions
            .Where(solution => solution.IsActive && solution.Packs.Any(pack => pack.IsActive))
            .Select(solution => solution.Id)
            .Distinct()
            .ToListAsync();

        return solutionIds.ToHashSet();
    }

    private async Task<string> ResolveUniqueSlugAsync(string? requestedSlug, string fallbackTitle, int? excludeId)
    {
        var baseSlug = SlugHelper.GenerateSlug(
            string.IsNullOrWhiteSpace(requestedSlug) ? fallbackTitle : requestedSlug);

        var candidate = baseSlug;
        var suffix = 2;

        while (await _context.Solutions.AnyAsync(solution =>
                   solution.Slug == candidate &&
                   (!excludeId.HasValue || solution.Id != excludeId.Value)))
        {
            var suffixText = "-" + suffix++;
            var maxBaseLength = Math.Max(1, 120 - suffixText.Length);
            candidate = baseSlug.Length > maxBaseLength
                ? baseSlug[..maxBaseLength].Trim('-') + suffixText
                : baseSlug + suffixText;
        }

        return candidate;
    }

    private async Task<int?> ResolveBaseSolutionIdAsync(
        SolutionType solutionType,
        int? baseSolutionId,
        int? currentSolutionId)
    {
        if (solutionType == SolutionType.General || !baseSolutionId.HasValue || baseSolutionId.Value <= 0)
        {
            return null;
        }

        if (currentSolutionId.HasValue && currentSolutionId.Value == baseSolutionId.Value)
        {
            throw new InvalidOperationException("A solution cannot reference itself as a base solution.");
        }

        var baseSolution = await _context.Solutions
            .AsNoTracking()
            .FirstOrDefaultAsync(solution => solution.Id == baseSolutionId.Value);

        if (baseSolution == null)
        {
            throw new InvalidOperationException("The selected base solution does not exist.");
        }

        if (baseSolution.Type != SolutionType.General)
        {
            throw new InvalidOperationException("Only general solutions can be selected as a base solution.");
        }

        return baseSolution.Id;
    }

    private void DeleteLocalSolutionImage(string? coverImageUrl)
    {
        var filePath = ResolveLocalSolutionImagePath(coverImageUrl);
        if (filePath == null)
        {
            return;
        }

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not delete solution image file {FilePath}", filePath);
        }
    }

    private string? ResolveLocalSolutionImagePath(string? coverImageUrl)
    {
        if (string.IsNullOrWhiteSpace(coverImageUrl))
        {
            return null;
        }

        var trimmed = coverImageUrl.Trim();
        if (!trimmed.StartsWith("/uploads/solutions/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var pathOnly = trimmed.Split('?', '#')[0].Replace('\\', '/');
        var fileName = Uri.UnescapeDataString(pathOnly["/uploads/solutions/".Length..]);
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains('/') || fileName.Contains('\\'))
        {
            return null;
        }

        var uploadRoot = Path.GetFullPath(SolutionUploadPath);
        var candidatePath = Path.GetFullPath(Path.Combine(uploadRoot, fileName));
        var uploadRootWithSeparator = uploadRoot.EndsWith(Path.DirectorySeparatorChar)
            ? uploadRoot
            : uploadRoot + Path.DirectorySeparatorChar;

        return candidatePath.StartsWith(uploadRootWithSeparator, StringComparison.OrdinalIgnoreCase)
            ? candidatePath
            : null;
    }

    private SolutionType ConvertStringToSolutionType(string typeString)
    {
        if (string.IsNullOrWhiteSpace(typeString))
        {
            throw new InvalidOperationException("Solution type cannot be empty.");
        }

        return typeString.ToLowerInvariant() switch
        {
            "general" => SolutionType.General,
            "sectorial" => SolutionType.Sectorial,
            _ => throw new InvalidOperationException(
                $"Invalid solution type: '{typeString}'. Must be 'General' or 'Sectorial'.")
        };
    }

    private string? ResolveSectorName(SolutionType solutionType, string? sectorName, List<string> sectors)
    {
        if (solutionType == SolutionType.General)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(sectorName))
        {
            return NormalizeRequiredSectorName(sectorName);
        }

        if (sectors.Any())
        {
            return sectors[0];
        }

        throw new InvalidOperationException("At least one sector is required for Sectorial solutions.");
    }

    private static string NormalizeRequiredSectorName(string sectorName)
    {
        var normalized = NormalizeOptionalSectorName(sectorName);
        if (normalized == null)
        {
            throw new InvalidOperationException("Sector name cannot be empty.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalSectorName(string? sectorName)
    {
        if (sectorName == null)
        {
            return null;
        }

        var trimmed = sectorName.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.Length > 80)
        {
            throw new InvalidOperationException("Sector names cannot exceed 80 characters.");
        }

        return trimmed;
    }

    private List<string> NormalizeSectorList(IEnumerable<string>? sectors)
    {
        return NormalizeStringList(sectors, 10, 80, "Sectors");
    }

    private static void AddSectorValue(ISet<string> sectors, string? sector)
    {
        if (string.IsNullOrWhiteSpace(sector))
        {
            return;
        }

        sectors.Add(sector.Trim());
    }

    private static List<string> NormalizeStringList(
        IEnumerable<string>? values,
        int maxItems,
        int maxLength,
        string fieldName)
    {
        if (values == null)
        {
            return new List<string>();
        }

        var cleaned = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (cleaned.Count > maxItems)
        {
            throw new InvalidOperationException($"{fieldName} cannot contain more than {maxItems} items.");
        }

        if (cleaned.Any(v => v.Length > maxLength))
        {
            throw new InvalidOperationException($"{fieldName} items cannot exceed {maxLength} characters.");
        }

        return cleaned;
    }

    private static List<SolutionUseCaseDto> NormalizeUseCases(IEnumerable<SolutionUseCaseDto>? useCases)
    {
        if (useCases == null)
        {
            return new List<SolutionUseCaseDto>();
        }

        var normalized = new List<SolutionUseCaseDto>();

        foreach (var item in useCases)
        {
            var title = item?.Title?.Trim() ?? string.Empty;
            var description = item?.Description?.Trim() ?? string.Empty;

            if (title.Length == 0 && description.Length == 0)
            {
                continue;
            }

            if (title.Length == 0)
            {
                throw new InvalidOperationException("Each use case must have a title.");
            }

            if (title.Length > 120)
            {
                throw new InvalidOperationException("Use case title cannot exceed 120 characters.");
            }

            if (description.Length > 600)
            {
                throw new InvalidOperationException("Use case description cannot exceed 600 characters.");
            }

            normalized.Add(new SolutionUseCaseDto
            {
                Title = title,
                Description = description
            });
        }

        if (normalized.Count > 30)
        {
            throw new InvalidOperationException("UseCases cannot contain more than 30 items.");
        }

        return normalized;
    }

    private static string? NormalizeCoverImageUrl(string? coverImageUrl)
    {
        if (coverImageUrl == null)
        {
            return null;
        }

        var trimmed = coverImageUrl.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.Length > 500)
        {
            throw new InvalidOperationException("CoverImageUrl cannot exceed 500 characters.");
        }

        var isAbsolute = Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        var isRelative = trimmed.StartsWith('/');
        if (!isAbsolute && !isRelative)
        {
            throw new InvalidOperationException("CoverImageUrl must be an absolute HTTP/HTTPS URL or a relative URL starting with '/'.");
        }

        return trimmed;
    }

    private static string? NormalizeYoutubeUrl(string? youtubeUrl)
    {
        if (youtubeUrl == null)
        {
            return null;
        }

        var trimmed = youtubeUrl.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.Length > 500)
        {
            throw new InvalidOperationException("YoutubeUrl cannot exceed 500 characters.");
        }

        var isAbsolute = Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        if (!isAbsolute)
        {
            throw new InvalidOperationException("YoutubeUrl must be an absolute HTTP/HTTPS URL.");
        }

        return trimmed;
    }

    private static string SerializeStringList(List<string> values)
    {
        return values.Count == 0 ? "[]" : JsonSerializer.Serialize(values);
    }

    private static string SerializeUseCases(List<SolutionUseCaseDto> useCases)
    {
        return useCases.Count == 0 ? "[]" : JsonSerializer.Serialize(useCases);
    }

    private static List<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static List<SolutionUseCaseDto> DeserializeUseCases(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<SolutionUseCaseDto>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<SolutionUseCaseDto>>(json) ?? new List<SolutionUseCaseDto>();
        }
        catch
        {
            return new List<SolutionUseCaseDto>();
        }
    }

    private SolutionDto MapToDto(Solution solution, ISet<int> activePackSolutionIds)
    {
        var sectors = DeserializeStringList(solution.SectorsJson);
        if (!string.IsNullOrWhiteSpace(solution.SectorName) &&
            !sectors.Contains(solution.SectorName, StringComparer.OrdinalIgnoreCase))
        {
            sectors.Insert(0, solution.SectorName);
        }

        var effectivePackSolution = ResolveEffectivePackSolution(solution, activePackSolutionIds);

        return new SolutionDto
        {
            Id = solution.Id,
            Title = solution.Title,
            Slug = solution.Slug,
            Description = solution.Description,
            Type = solution.Type.ToString(),
            SectorName = solution.SectorName,
            BaseSolutionId = solution.BaseSolutionId,
            PackIconKey = string.IsNullOrWhiteSpace(solution.PackIconKey)
                ? PackPresentationCatalog.DefaultSolutionIconKey
                : solution.PackIconKey,
            PackThemeKey = string.IsNullOrWhiteSpace(solution.PackThemeKey)
                ? PackPresentationCatalog.DefaultSolutionThemeKey
                : solution.PackThemeKey,
            HasPacks = effectivePackSolution != null,
            EffectivePackSolutionSlug = effectivePackSolution?.Slug,
            CoverImageUrl = solution.CoverImageUrl,
            YoutubeUrl = solution.YoutubeUrl,
            Sectors = sectors,
            TopClients = DeserializeStringList(solution.TopClientsJson),
            Functionalities = DeserializeStringList(solution.FunctionalitiesJson),
            Advantages = DeserializeStringList(solution.AdvantagesJson),
            UseCases = DeserializeUseCases(solution.UseCasesJson),
            IsActive = solution.IsActive,
            CreatedByName = solution.CreatedByUser != null
                ? $"{solution.CreatedByUser.FirstName} {solution.CreatedByUser.LastName}".Trim()
                : "Unknown",
            CreatedAt = solution.CreatedAt,
            UpdatedAt = solution.UpdatedAt
        };
    }

    private static Solution? ResolveEffectivePackSolution(Solution solution, ISet<int> activePackSolutionIds)
    {
        if (solution.Type == SolutionType.General)
        {
            return solution.IsActive && activePackSolutionIds.Contains(solution.Id)
                ? solution
                : null;
        }

        if (solution.BaseSolution != null &&
            solution.BaseSolution.IsActive &&
            activePackSolutionIds.Contains(solution.BaseSolution.Id))
        {
            return solution.BaseSolution;
        }

        return null;
    }
}
