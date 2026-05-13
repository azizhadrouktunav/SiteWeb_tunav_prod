using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tunav_backend.Models;

public enum RegistrationStatus
{
    Nouvelle = 0,
    Confirmee = 1,
    Annulee = 2
}

public class EventRegistration
{
    public int Id { get; set; }

    [Required]
    public int EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Organization { get; set; }

    [MaxLength(500)]
    public string? Message { get; set; }

    public RegistrationStatus Status { get; set; } = RegistrationStatus.Nouvelle;

    [MaxLength(500)]
    public string? InternalNote { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [NotMapped]
    public string EventTitle => Event?.Title ?? "—";
}