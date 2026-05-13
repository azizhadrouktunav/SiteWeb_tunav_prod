namespace tunav_backend.Models;

// ── Réclamation partenaire (traitée par SAV) ──────────────────────────────────
public class PartnerClaim
{
    public int Id { get; set; }

    // Partenaire backoffice = User avec rôle "Partenaire"
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normale";   // Basse / Normale / Haute / Urgente

    public string Status { get; set; } = "Nouvelle"; // Nouvelle / En cours / Résolue / Fermée
    public string? SavNote { get; set; }
    public int? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

// ── Nouvelle demande partenaire (traitée par Commercial) ─────────────────────
public class PartnerDemand
{
    public int Id { get; set; }

    // Partenaire backoffice = User avec rôle "Partenaire"
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string DemandType { get; set; } = string.Empty; // Nouveau client / Démonstration / Support commercial / Autre
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }

    public string Status { get; set; } = "Nouvelle"; // Nouvelle / En traitement / Acceptée / Refusée / Clôturée
    public string? CommercialNote { get; set; }
    public int? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}