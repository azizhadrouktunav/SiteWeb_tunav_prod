using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tunav_backend.Models
{
    // ── Établissement partenaire formation ────────────────────────────────────
    public class TrainingPartner
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Domain { get; set; } = string.Empty;   // ex : "Data Science"

        [MaxLength(10)]
        public string? Icon { get; set; }                    // emoji ex : "🚀"

        /// <summary>URL image personnalisée (prioritaire sur l'image auto-générée)</summary>
        [MaxLength(2000)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    // ── Témoignage employé ────────────────────────────────────────────────────
    public class Testimonial
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string AuthorName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string AuthorRole { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Company { get; set; }                 // ex : "TUNAV IT Groupe"

        [MaxLength(10)]
        public string? Avatar { get; set; }                  // emoji ex : "👩‍💻"

        [Required, MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public int Rating { get; set; } = 5;                 // 1–5

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}