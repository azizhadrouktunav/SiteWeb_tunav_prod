using System.ComponentModel.DataAnnotations;

namespace tunav_backend.DTOs;

public class TeamMemberDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Position { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Email { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>US-PE08 — Ajout</summary>
public class CreateTeamMemberDto
{
    [Required(ErrorMessage = "Le prénom est obligatoire")]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nom est obligatoire")]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le poste est obligatoire")]
    [StringLength(150, MinimumLength = 2)]
    public string Position { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    [StringLength(500)]
    public string? LinkedInUrl { get; set; }

    [StringLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    public int DisplayOrder { get; set; } = 0;
}

/// <summary>US-PE09 — Modification</summary>
public class UpdateTeamMemberDto
{
    [StringLength(100, MinimumLength = 2)]
    public string? FirstName { get; set; }

    [StringLength(100, MinimumLength = 2)]
    public string? LastName { get; set; }

    [StringLength(150, MinimumLength = 2)]
    public string? Position { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    [StringLength(500)]
    public string? LinkedInUrl { get; set; }

    [StringLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    public int? DisplayOrder { get; set; }

    /// <summary>US-PE11</summary>
    public bool? IsActive { get; set; }
}
