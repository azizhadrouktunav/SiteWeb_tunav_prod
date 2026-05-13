using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tunav_backend.Models
{
    public enum JobContractType
    {
        CDI = 0,
        CDD = 1,
        Stage = 2,
        Freelance = 3,
        Alternance = 4
    }

    public enum JobAcademicLevel
    {
        Tous = 0,
        Bac = 1,
        Licence = 2,
        Ingenieur = 3,
        Master = 4,
        Doctorat = 5
    }

    public enum JobPostType
    {
        Emploi = 0,
        Stage = 1,
        StageEte = 2,
        PFE = 3
    }

    public enum JobStatus
    {
        Active = 0,
        Inactive = 1,
        Archived = 2
    }

    public enum ApplicationStatus
    {
        Nouvelle = 0,
        EnCoursEtude = 1,
        Entretien = 2,
        Acceptee = 3,
        Refusee = 4
    }

    // ── Offre d'emploi / stage ────────────────────────────────────────────────
    public class JobOffer
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(3000)]
        public string? Description { get; set; }

        [MaxLength(2000)]
        public string? Requirements { get; set; }

        [Required]
        public JobContractType ContractType { get; set; } = JobContractType.CDI;

        [Required]
        public JobAcademicLevel AcademicLevel { get; set; } = JobAcademicLevel.Tous;

        [Required]
        public JobPostType PostType { get; set; } = JobPostType.Emploi;

        [Required]
        public JobStatus Status { get; set; } = JobStatus.Active;

        [MaxLength(150)]
        public string? Location { get; set; }

        [MaxLength(100)]
        public string? Duration { get; set; }         // ex: "3-5 ans", "2-3 mois"

        [MaxLength(150)]
        public string? Salary { get; set; }           // ex: "Compétitif selon expérience"

        /// <summary>Compétences requises séparées par virgule</summary>
        [MaxLength(500)]
        public string? Skills { get; set; }

        /// <summary>Missions principales (une par ligne)</summary>
        [MaxLength(3000)]
        public string? Missions { get; set; }

        /// <summary>Ce que nous offrons — avantages (une par ligne)</summary>
        [MaxLength(3000)]
        public string? Benefits { get; set; }

        /// <summary>Processus de recrutement — étapes (une par ligne)</summary>
        [MaxLength(2000)]
        public string? Process { get; set; }

        public DateTime? Deadline { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsArchived { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public User? CreatedByUser { get; set; }

        public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();

        [NotMapped]
        public string CreatedByName => CreatedByUser is null
            ? "—"
            : $"{CreatedByUser.FirstName} {CreatedByUser.LastName}";

        [NotMapped]
        public bool IsExpired => Deadline.HasValue && Deadline.Value < DateTime.UtcNow;

        [NotMapped]
        public string[] SkillList
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Skills))
                    return Array.Empty<string>();

                // Si la string contient des virgules → split sur virgule
                if (Skills.Contains(','))
                    return Skills.Split(',')
                                 .Select(s => s.Trim())
                                 .Where(s => s.Length > 0)
                                 .ToArray();

                // Si la string contient des points-virgules → split sur ;
                if (Skills.Contains(';'))
                    return Skills.Split(';')
                                 .Select(s => s.Trim())
                                 .Where(s => s.Length > 0)
                                 .ToArray();

                // Aucun séparateur → retourner tel quel comme un seul skill
                // (l'admin doit corriger via le backoffice)
                return new[] { Skills.Trim() };
            }
        }
    }

    // ── Candidature ───────────────────────────────────────────────────────────
    public class JobApplication
    {
        public int Id { get; set; }

        public int JobOfferId { get; set; }

        [ForeignKey(nameof(JobOfferId))]
        public JobOffer? JobOffer { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        /// <summary>Nom original|guid.pdf du CV</summary>
        [MaxLength(500)]
        public string? CvFile { get; set; }

        /// <summary>Nom original|guid.pdf de la lettre de motivation</summary>
        [MaxLength(500)]
        public string? MotivationLetterFile { get; set; }

        [MaxLength(1000)]
        public string? Message { get; set; }

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Nouvelle;

        [MaxLength(500)]
        public string? InternalNote { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [NotMapped]
        public string JobTitle => JobOffer?.Title ?? "—";

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}