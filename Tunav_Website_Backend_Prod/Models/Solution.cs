namespace tunav_backend.Models;

/// <summary>
/// Solution entity for the Solutions module (US-GS09)
/// Represents a solution that can be published on the site
/// </summary>
public class Solution
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Solution title (required, max 255 characters)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Public slug used for packs deep-linking and public navigation.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Solution description (optional)
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of solution: General or Sectorial
    /// </summary>
    public SolutionType Type { get; set; } = SolutionType.General;

    /// <summary>
    /// If type is Sectorial, specify the sector name (e.g., "Healthcare", "Tourism")
    /// </summary>
    public string? SectorName { get; set; }

    /// <summary>
    /// Optional base/general solution used by sectorial solutions to inherit packs.
    /// </summary>
    public int? BaseSolutionId { get; set; }

    /// <summary>
    /// Icon key used by the packs page solution pills.
    /// </summary>
    public string PackIconKey { get; set; } = "map-pin";

    /// <summary>
    /// Theme key used by the packs page hero pill and solution badge.
    /// </summary>
    public string PackThemeKey { get; set; } = "blue-cyan";

    /// <summary>
    /// Cover image URL displayed in cards/detail pages
    /// </summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// YouTube video URL shown in solution details
    /// </summary>
    public string? YoutubeUrl { get; set; }

    /// <summary>
    /// Serialized JSON array of sectors/tags
    /// </summary>
    public string? SectorsJson { get; set; }

    /// <summary>
    /// Serialized JSON array of top client names
    /// </summary>
    public string? TopClientsJson { get; set; }

    /// <summary>
    /// Serialized JSON array of functionalities
    /// </summary>
    public string? FunctionalitiesJson { get; set; }

    /// <summary>
    /// Serialized JSON array of advantages
    /// </summary>
    public string? AdvantagesJson { get; set; }

    /// <summary>
    /// Serialized JSON array of use cases (title + description)
    /// </summary>
    public string? UseCasesJson { get; set; }

    /// <summary>
    /// Indicates if solution is active and can be displayed
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Foreign key to User who created the solution
    /// </summary>
    public int CreatedBy { get; set; }

    /// <summary>
    /// Date and time when solution was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time of last update (UTC, nullable)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property: User who created the solution
    /// </summary>
    public User? CreatedByUser { get; set; }

    /// <summary>
    /// Navigation property to the base/general solution used for pack inheritance.
    /// </summary>
    public Solution? BaseSolution { get; set; }

    /// <summary>
    /// Navigation property to sectorial solutions derived from this solution.
    /// </summary>
    public ICollection<Solution> DerivedSolutions { get; set; } = new List<Solution>();

    /// <summary>
    /// Navigation property to packs owned by this general solution.
    /// </summary>
    public ICollection<Pack> Packs { get; set; } = new List<Pack>();
}

