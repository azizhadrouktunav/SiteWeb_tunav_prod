using System.ComponentModel.DataAnnotations;

namespace tunav_backend.Models
{
    public enum DemoRequestEntryPoint
    {
        DemoPage = 0,
        SolutionsList = 1,
        SolutionDetail = 2,
        PacksPage = 3
    }

    public enum DemoRequestStatus
    {
        Nouvelle = 0,
        EnCours = 1,
        Acceptee = 2,
        Refusee = 3
    }

    public class DemoRequest
    {
        public int Id { get; set; }

        [Required]
        public int SolutionId { get; set; }

        public int? PackId { get; set; }

        [Required, MaxLength(255)]
        public string SolutionTitle { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? PackName { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public bool HasWhatsapp { get; set; }

        [MaxLength(30)]
        public string? WhatsappNumber { get; set; }

        [Required]
        public DemoRequestEntryPoint EntryPoint { get; set; } = DemoRequestEntryPoint.DemoPage;

        [Required]
        public DemoRequestStatus Status { get; set; } = DemoRequestStatus.Nouvelle;

        [MaxLength(500)]
        public string? InternalNote { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}