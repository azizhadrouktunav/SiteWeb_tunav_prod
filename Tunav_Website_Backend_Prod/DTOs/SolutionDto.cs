namespace tunav_backend.DTOs;

/// <summary>
/// Data Transfer Object for Solution response
/// </summary>
public class SolutionDto
{
    /// <summary>
    /// Solution ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Solution title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Public slug used for packs deep-linking.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Solution description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type: "General" or "Sectorial"
    /// </summary>
    public string Type { get; set; } = "General";

    /// <summary>
    /// Sector name if type is Sectorial
    /// </summary>
    public string? SectorName { get; set; }

    /// <summary>
    /// Optional base/general solution id used to inherit packs.
    /// </summary>
    public int? BaseSolutionId { get; set; }

    /// <summary>
    /// Icon key used by the packs page.
    /// </summary>
    public string PackIconKey { get; set; } = string.Empty;

    /// <summary>
    /// Theme key used by the packs page.
    /// </summary>
    public string PackThemeKey { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this solution resolves to an active pack family.
    /// </summary>
    public bool HasPacks { get; set; }

    /// <summary>
    /// Slug of the effective general solution that owns the packs for this solution.
    /// </summary>
    public string? EffectivePackSolutionSlug { get; set; }

    /// <summary>
    /// Cover image URL displayed in cards/details
    /// </summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// YouTube video URL displayed in detail page
    /// </summary>
    public string? YoutubeUrl { get; set; }

    /// <summary>
    /// Sector tags displayed in cards/details
    /// </summary>
    public List<string> Sectors { get; set; } = new List<string>();

    /// <summary>
    /// Top clients displayed in cards/details
    /// </summary>
    public List<string> TopClients { get; set; } = new List<string>();

    /// <summary>
    /// Functionalities displayed in detail page
    /// </summary>
    public List<string> Functionalities { get; set; } = new List<string>();

    /// <summary>
    /// Advantages displayed in detail page
    /// </summary>
    public List<string> Advantages { get; set; } = new List<string>();

    /// <summary>
    /// Use cases displayed in detail page
    /// </summary>
    public List<SolutionUseCaseDto> UseCases { get; set; } = new List<SolutionUseCaseDto>();

    /// <summary>
    /// Whether solution is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Name of user who created the solution
    /// </summary>
    public string CreatedByName { get; set; } = string.Empty;

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

