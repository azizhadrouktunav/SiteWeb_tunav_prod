using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tunav_backend.Models
{
    // ── Catégorie de blog ─────────────────────────────────────────────────────
    public class BlogCategory
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<BlogArticle> Articles { get; set; } = new List<BlogArticle>();
    }

    // ── Article de blog ───────────────────────────────────────────────────────
    public class BlogArticle
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Summary { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }

        /// <summary>URL ou ID d'une vidéo YouTube liée à l'article (optionnel)</summary>
        [MaxLength(500)]
        public string? YoutubeUrl { get; set; }

        /// <summary>Secteur / thématique de l'article (ex: Transport, Santé, Agriculture…)</summary>
        [MaxLength(100)]
        public string? Sector { get; set; }

        /// <summary>Date de publication choisie par le rédacteur (null = brouillon non publié)</summary>
        public DateTime? PublishedAt { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // FK Catégorie
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public BlogCategory? Category { get; set; }

        // FK Auteur (User)
        public int? CreatedById { get; set; }

        [ForeignKey(nameof(CreatedById))]
        public User? CreatedBy { get; set; }

        [NotMapped]
        public string? CreatedByName => CreatedBy is null
            ? null
            : $"{CreatedBy.FirstName} {CreatedBy.LastName}";

        [NotMapped]
        public string? CategoryName => Category?.Name;

        /// <summary>Extrait l'ID YouTube depuis une URL ou un ID brut pour l'embed iframe</summary>
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
}