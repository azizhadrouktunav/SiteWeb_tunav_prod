using System.ComponentModel.DataAnnotations;

namespace tunav_backend.DTOs;

/// <summary>
/// Data Transfer Object for updating an existing solution
/// </summary>
public class UpdateSolutionDto
{
    /// <summary>
    /// Solution title (optional)
    /// </summary>
    [StringLength(255, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 255 characters")]
    public string? Title { get; set; }

    /// <summary>
    /// Optional public slug. Generated from the title when omitted.
    /// </summary>
    [StringLength(120, ErrorMessage = "Slug cannot exceed 120 characters")]
    public string? Slug { get; set; }

    /// <summary>
    /// Solution description (optional)
    /// </summary>
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Type: "General" or "Sectorial" (optional)
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Sector name if type is Sectorial (optional)
    /// </summary>
    public string? SectorName { get; set; }

    /// <summary>
    /// Optional base/general solution used for sectorial pack inheritance.
    /// </summary>
    public int? BaseSolutionId { get; set; }

    /// <summary>
    /// Explicitly clears the inherited base/general solution for sectorial solutions.
    /// </summary>
    public bool? ClearBaseSolution { get; set; }

    /// <summary>
    /// Presentation icon key used on the packs page.
    /// </summary>
    [StringLength(50, ErrorMessage = "PackIconKey cannot exceed 50 characters")]
    public string? PackIconKey { get; set; }

    /// <summary>
    /// Presentation theme key used on the packs page.
    /// </summary>
    [StringLength(50, ErrorMessage = "PackThemeKey cannot exceed 50 characters")]
    public string? PackThemeKey { get; set; }

    /// <summary>
    /// Cover image URL of the solution card/detail (optional)
    /// </summary>
    [StringLength(500, ErrorMessage = "CoverImageUrl cannot exceed 500 characters")]
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// YouTube video URL shown in detail page (optional)
    /// </summary>
    [StringLength(500, ErrorMessage = "YoutubeUrl cannot exceed 500 characters")]
    public string? YoutubeUrl { get; set; }

    /// <summary>
    /// Sector tags shown on cards/details (optional)
    /// </summary>
    public List<string>? Sectors { get; set; }

    /// <summary>
    /// Top clients shown on cards/details (optional)
    /// </summary>
    public List<string>? TopClients { get; set; }

    /// <summary>
    /// Functionalities shown in detail page (optional)
    /// </summary>
    public List<string>? Functionalities { get; set; }

    /// <summary>
    /// Advantages shown in detail page (optional)
    /// </summary>
    public List<string>? Advantages { get; set; }

    /// <summary>
    /// Use cases shown in detail page (optional)
    /// </summary>
    public List<SolutionUseCaseDto>? UseCases { get; set; }

    /// <summary>
    /// Whether solution is active (optional)
    /// </summary>
    public bool? IsActive { get; set; }
}

