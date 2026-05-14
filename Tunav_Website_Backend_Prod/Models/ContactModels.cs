using System.ComponentModel.DataAnnotations;

namespace tunav_backend.Models;

// ── Demande de contact (formulaire principal) ─────────────────────────────────
public class ContactRequest
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = "";

    [Required, MaxLength(100)]
    public string LastName { get; set; } = "";

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = "";

    [MaxLength(20)]
    public string Phone { get; set; } = "";

    [MaxLength(200)]
    public string? Company { get; set; }

    [Required, MaxLength(200)]
    public string Subject { get; set; } = "";

    /// <summary>Rempli uniquement si Subject = "Autre"</summary>
    [MaxLength(200)]
    public string? OtherSubject { get; set; }

    [Required, MaxLength(3000)]
    public string Message { get; set; } = "";

    public bool Consent { get; set; }

    /// <summary>Nouveau | EnCours | Traité</summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Nouveau";

    [MaxLength(500)]
    public string? InternalNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Payload du modal « Rendez-vous / démo » sur la page Contact (public).</summary>
public class ContactDemoSubmitDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = "";

    [Required, MaxLength(100)]
    public string LastName { get; set; } = "";

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = "";

    public bool HasWhatsApp { get; set; }

    [MaxLength(40)]
    public string? WhatsAppNumber { get; set; }
}