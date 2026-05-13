using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tunav_backend.Models
{
    public class Newsletter
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Summary { get; set; }

        /// <summary>Contenu du sommaire — une ligne par item</summary>
        [MaxLength(2000)]
        public string? TableOfContents { get; set; }

        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }

        /// <summary>URL ou chemin du PDF à télécharger</summary>
        [MaxLength(500)]
        public string? PdfUrl { get; set; }

        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public User? CreatedByUser { get; set; }

        [NotMapped]
        public string[] TocLines => string.IsNullOrWhiteSpace(TableOfContents)
            ? Array.Empty<string>()
            : TableOfContents.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
    }

    // ── Abonné à la newsletter ────────────────────────────────────────────────
    public class NewsletterSubscriber
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }   // WhatsApp optionnel

        public bool IsActive { get; set; } = true;

        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Token unique pour le désabonnement en 1 clic</summary>
        [MaxLength(100)]
        public string UnsubscribeToken { get; set; } = Guid.NewGuid().ToString("N");
    }
}