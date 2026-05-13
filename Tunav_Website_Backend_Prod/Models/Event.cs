using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tunav_backend.Models
{
    public enum EventStatus
    {
        Draft = 0,
        Published = 1,
        Archived = 2
    }

    public enum EventType
    {
        Salon = 0,
        Conference = 1,
        Partenariat = 2,
        Interne = 3,
        Autre = 4
    }

    public enum CollaborationStatus
    {
        Nouvelle = 0,
        EnCours = 1,
        Acceptee = 2,
        Refusee = 3
    }

    /// <summary>
    /// Distingue l'origine / la nature de la demande reçue.
    /// Collaboration        → Formulaire 1 du Pôle Formation (Demande de collaboration)
    /// PropositionEvenement → Formulaire de la page Événements
    /// DemandeFormation     → Formulaire 2 du Pôle Formation (Demande de formation)
    /// </summary>
    public enum CollaborationType
    {
        Collaboration = 0,          // Pôle Formation — form 1
        PropositionEvenement = 1,   // Page Events
        DemandeFormation = 2        // Pôle Formation — form 2
    }

    public class Event
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public EventType Type { get; set; } = EventType.Autre;

        [Required]
        public EventStatus Status { get; set; } = EventStatus.Draft;

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [MaxLength(300)]
        public string? Location { get; set; }

        [MaxLength(500)]
        public string? OnlineLink { get; set; }

        public int? ParticipantCount { get; set; }

        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }

        [MaxLength(500)]
        public string? YoutubeUrl { get; set; }

        [MaxLength(500)]
        public string? ExternalUrl { get; set; }

        public bool IsArchived { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public User? CreatedByUser { get; set; }

        public ICollection<CollaborationRequest> CollaborationRequests { get; set; }
            = new List<CollaborationRequest>();

        // ── AJOUT : navigation vers les inscriptions ──────────────────────────
        public ICollection<EventRegistration> Registrations { get; set; }
            = new List<EventRegistration>();

        [NotMapped]
        public string CreatedByName => CreatedByUser is null
            ? "—"
            : $"{CreatedByUser.FirstName} {CreatedByUser.LastName}";

        [NotMapped]
        public bool IsUpcoming => StartDate > DateTime.UtcNow && Status == EventStatus.Published;

        [NotMapped]
        public string? YoutubeEmbedId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(YoutubeUrl)) return null;
                var url = YoutubeUrl.Trim();
                if (url.Contains("youtu.be/"))
                    return url.Split("youtu.be/").Last().Split('?').First();
                if (url.Contains("v="))
                    return url.Split("v=").Last().Split('&').First();
                return url.Length <= 20 ? url : null;
            }
        }
    }

    public class CollaborationRequest
    {
        public int Id { get; set; }

        // EventId est nullable — une collaboration peut être indépendante d'un événement
        public int? EventId { get; set; }

        [ForeignKey(nameof(EventId))]
        public Event? Event { get; set; }

        // ── Type de demande (distingue les 3 onglets du backoffice) ──────────
        public CollaborationType CollaborationType { get; set; } = CollaborationType.Collaboration;

        /// <summary>Nom de l'organisme</summary>
        [Required, MaxLength(150)]
        public string Organization { get; set; } = string.Empty;

        /// <summary>Nom du responsable</summary>
        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Address { get; set; }

        /// <summary>Description / intitulé de l'événement proposé ou de la formation</summary>
        [MaxLength(1000)]
        public string? Message { get; set; }

        /// <summary>Noms des pièces jointes uploadées (CSV)</summary>
        [MaxLength(500)]
        public string? AttachmentNames { get; set; }

        // ── Champ spécifique Pôle Formation ─────────────────────────────────
        /// <summary>Homologation MALEK – CNFCPP (uniquement pour DemandeFormation)</summary>
        public bool? IsHomologueMalek { get; set; }

        public CollaborationStatus Status { get; set; } = CollaborationStatus.Nouvelle;

        [MaxLength(500)]
        public string? InternalNote { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [NotMapped]
        public string EventTitle => Event?.Title ?? "—";
    }
}