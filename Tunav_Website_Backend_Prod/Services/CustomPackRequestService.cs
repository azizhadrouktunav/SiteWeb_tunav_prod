using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Services;

public record CustomPackRequestDto(
    int Id,
    int SolutionId,
    string SolutionTitle,
    string ContactName,
    string Company,
    string Email,
    string Phone,
    string? Message,
    List<string> SelectedFeatures,
    string Status,
    string? InternalNote,
    DateTime SubmittedAt,
    DateTime? UpdatedAt);

public record CreateCustomPackRequestDto(
    int SolutionId,
    string ContactName,
    string Company,
    string Email,
    string Phone,
    string? Message,
    List<string>? SelectedFeatures);

public interface ICustomPackRequestService
{
    Task<List<CustomPackRequestDto>> GetAllAsync(string? status = null, int? solutionId = null, string? search = null);
    Task<CustomPackRequestDto?> GetByIdAsync(int id);
    Task<CustomPackRequestDto> CreateAsync(CreateCustomPackRequestDto dto);
    Task<CustomPackRequestDto?> UpdateStatusAsync(int id, string status, string? note);
    Task<bool> DeleteAsync(int id);
}

public class CustomPackRequestService : ICustomPackRequestService
{
    private readonly AppDbContext _db;

    public CustomPackRequestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CustomPackRequestDto>> GetAllAsync(string? status = null, int? solutionId = null, string? search = null)
    {
        var query = _db.CustomPackRequests
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<CustomPackRequestStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(item => item.Status == parsedStatus);
        }

        if (solutionId.HasValue && solutionId.Value > 0)
        {
            query = query.Where(item => item.SolutionId == solutionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item =>
                EF.Functions.Like(item.SolutionTitle, $"%{term}%") ||
                EF.Functions.Like(item.ContactName, $"%{term}%") ||
                EF.Functions.Like(item.Company, $"%{term}%") ||
                EF.Functions.Like(item.Email, $"%{term}%"));
        }

        var items = await query
            .OrderByDescending(item => item.SubmittedAt)
            .ToListAsync();

        return items.Select(ToDto).ToList();
    }

    public async Task<CustomPackRequestDto?> GetByIdAsync(int id)
    {
        var item = await _db.CustomPackRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(request => request.Id == id);
        return item == null ? null : ToDto(item);
    }

    public async Task<CustomPackRequestDto> CreateAsync(CreateCustomPackRequestDto dto)
    {
        var effectiveSolution = await ResolveEffectivePackSolutionAsync(dto.SolutionId);
        var activePacks = await _db.Packs
            .AsNoTracking()
            .Where(pack => pack.SolutionId == effectiveSolution.Id && pack.IsActive)
            .ToListAsync();

        if (activePacks.Count == 0)
        {
            throw new InvalidOperationException("La solution sélectionnée ne possède pas de packs actifs.");
        }

        var selectedFeatures = NormalizeSelectedFeatures(dto.SelectedFeatures);
        var allowedFeatures = activePacks
            .SelectMany(pack => DeserializeFeatures(pack.FeaturesJson))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectedFeatures.Any(feature => !allowedFeatures.Contains(feature)))
        {
            throw new InvalidOperationException("Une ou plusieurs fonctionnalités sélectionnées sont invalides.");
        }

        var item = new CustomPackRequest
        {
            SolutionId = effectiveSolution.Id,
            SolutionTitle = effectiveSolution.Title,
            ContactName = NormalizeContactName(dto.ContactName),
            Company = NormalizeCompany(dto.Company),
            Email = NormalizeEmail(dto.Email),
            Phone = NormalizePhone(dto.Phone),
            Message = NormalizeOptionalMessage(dto.Message),
            SelectedFeaturesJson = SerializeFeatures(selectedFeatures),
            Status = CustomPackRequestStatus.Nouvelle,
            SubmittedAt = DateTime.UtcNow
        };

        _db.CustomPackRequests.Add(item);
        await _db.SaveChangesAsync();

        return ToDto(item);
    }

    public async Task<CustomPackRequestDto?> UpdateStatusAsync(int id, string status, string? note)
    {
        if (!Enum.TryParse<CustomPackRequestStatus>(status, true, out var parsedStatus))
        {
            throw new InvalidOperationException($"Statut invalide : '{status}'.");
        }

        var item = await _db.CustomPackRequests.FindAsync(id);
        if (item == null)
        {
            return null;
        }

        item.Status = parsedStatus;
        item.InternalNote = NormalizeInternalNote(note);
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _db.CustomPackRequests.FindAsync(id);
        if (item == null)
        {
            return false;
        }

        _db.CustomPackRequests.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<Solution> ResolveEffectivePackSolutionAsync(int solutionId)
    {
        var solution = await _db.Solutions
            .Include(item => item.BaseSolution)
            .FirstOrDefaultAsync(item => item.Id == solutionId && item.IsActive);

        if (solution == null)
        {
            throw new InvalidOperationException("La solution sélectionnée est invalide ou inactive.");
        }

        if (solution.Type == SolutionType.General)
        {
            return solution;
        }

        if (solution.BaseSolution == null || !solution.BaseSolution.IsActive)
        {
            throw new InvalidOperationException("Cette solution ne référence aucun catalogue de packs actif.");
        }

        return solution.BaseSolution;
    }

    private static List<string> NormalizeSelectedFeatures(IEnumerable<string>? values)
    {
        var selectedFeatures = (values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (selectedFeatures.Count == 0)
        {
            throw new InvalidOperationException("Veuillez sélectionner au moins une fonctionnalité.");
        }

        if (selectedFeatures.Count > 80)
        {
            throw new InvalidOperationException("Vous ne pouvez pas sélectionner plus de 80 fonctionnalités.");
        }

        return selectedFeatures;
    }

    private static string? NormalizeInternalNote(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return null;
        }

        if (normalized.Length > 500)
        {
            throw new InvalidOperationException("La note interne ne peut pas depasser 500 caracteres.");
        }

        return normalized;
    }

    private static string NormalizeContactName(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length < 2 || normalized.Length > 160)
        {
            throw new InvalidOperationException("Le nom est obligatoire.");
        }

        return normalized;
    }

    private static string NormalizeCompany(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length < 2 || normalized.Length > 200)
        {
            throw new InvalidOperationException("Le nom de l'entreprise est obligatoire.");
        }

        return normalized;
    }

    private static string NormalizeEmail(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            throw new InvalidOperationException("L'adresse email est obligatoire.");
        }

        if (!normalized.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Veuillez saisir une adresse email valide.");
        }

        if (normalized.Length > 200)
        {
            throw new InvalidOperationException("L'adresse email ne peut pas dépasser 200 caractères.");
        }

        return normalized;
    }

    private static string NormalizePhone(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length < 6 || normalized.Length > 30)
        {
            throw new InvalidOperationException("Le téléphone est obligatoire.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalMessage(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return null;
        }

        if (normalized.Length > 2000)
        {
            throw new InvalidOperationException("Le message ne peut pas dépasser 2000 caractères.");
        }

        return normalized;
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

    private static string SerializeFeatures(List<string> features)
    {
        var json = JsonSerializer.Serialize(features);
        if (json.Length > 12000)
        {
            throw new InvalidOperationException("La liste des fonctionnalites selectionnees est trop longue.");
        }

        return json;
    }

    private static CustomPackRequestDto ToDto(CustomPackRequest item) => new(
        item.Id,
        item.SolutionId,
        item.SolutionTitle,
        item.ContactName,
        item.Company,
        item.Email,
        item.Phone,
        item.Message,
        DeserializeFeatures(item.SelectedFeaturesJson),
        item.Status.ToString(),
        item.InternalNote,
        item.SubmittedAt,
        item.UpdatedAt);
}
