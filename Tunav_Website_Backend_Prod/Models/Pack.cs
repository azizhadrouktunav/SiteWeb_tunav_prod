using System.ComponentModel.DataAnnotations;

namespace tunav_backend.Models;

public class Pack
{
    public int Id { get; set; }

    [Required]
    public int SolutionId { get; set; }

    [Required, MaxLength(180)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(1200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string FeaturesJson { get; set; } = "[]";

    [Required, MaxLength(50)]
    public string ThemeKey { get; set; } = "green";

    [MaxLength(500)]
    public string? VideoUrl { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsPopular { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Solution? Solution { get; set; }
}
