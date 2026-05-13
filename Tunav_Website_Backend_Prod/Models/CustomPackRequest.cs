using System.ComponentModel.DataAnnotations;

namespace tunav_backend.Models;

public enum CustomPackRequestStatus
{
    Nouvelle = 0,
    EnCours = 1,
    Acceptee = 2,
    Refusee = 3
}

public class CustomPackRequest
{
    public int Id { get; set; }

    [Required]
    public int SolutionId { get; set; }

    [Required, MaxLength(255)]
    public string SolutionTitle { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string ContactName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Company { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Message { get; set; }

    [Required]
    public string SelectedFeaturesJson { get; set; } = "[]";

    [Required]
    public CustomPackRequestStatus Status { get; set; } = CustomPackRequestStatus.Nouvelle;

    [MaxLength(500)]
    public string? InternalNote { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
