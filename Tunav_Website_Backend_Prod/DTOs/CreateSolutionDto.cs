using System.ComponentModel.DataAnnotations;

namespace tunav_backend.DTOs;

/// <summary>
/// Data Transfer Object for creating a new solution
/// </summary>
public class CreateSolutionDto
{
    /// <summary>
    /// Solution title (required, 3-255 characters)
    /// </summary>
    [Required(ErrorMessage = "Title is required")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 255 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional public slug. Generated from the title when omitted.
    /// </summary>
    [StringLength(120, ErrorMessage = "Slug cannot exceed 120 characters")]
    public string? Slug { get; set; }

    /// <summary>
    /// Solution description (optional)
    /// </summary>
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type: "General" or "Sectorial" (required)
    /// </summary>
    [Required(ErrorMessage = "Type is required")]
    public string Type { get; set; } = "General";

    /// <summary>
    /// Sector name if type is Sectorial (required for Sectorial type)
    /// </summary>
    public string? SectorName { get; set; }

    /// <summary>
    /// Optional base/general solution used for sectorial pack inheritance.
    /// </summary>
    public int? BaseSolutionId { get; set; }

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
    /// Cover image URL of the solution card/detail
    /// </summary>
    [StringLength(500, ErrorMessage = "CoverImageUrl cannot exceed 500 characters")]
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// YouTube video URL shown in detail page
    /// </summary>
    [StringLength(500, ErrorMessage = "YoutubeUrl cannot exceed 500 characters")]
    public string? YoutubeUrl { get; set; }

    /// <summary>
    /// Sector tags shown on cards/details
    /// </summary>
    public List<string>? Sectors { get; set; } = new List<string>();

    /// <summary>
    /// Top clients shown on cards/details
    /// </summary>
    public List<string>? TopClients { get; set; } = new List<string>();

    /// <summary>
    /// Functionalities shown in detail page
    /// </summary>
    public List<string>? Functionalities { get; set; } = new List<string>();

    /// <summary>
    /// Advantages shown in detail page
    /// </summary>
    public List<string>? Advantages { get; set; } = new List<string>();

    /// <summary>
    /// Use cases shown in detail page
    /// </summary>
    public List<SolutionUseCaseDto>? UseCases { get; set; } = new List<SolutionUseCaseDto>();
}

