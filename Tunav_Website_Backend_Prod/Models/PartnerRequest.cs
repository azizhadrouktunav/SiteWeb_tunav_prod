using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace tunav_backend.Models
{
    public enum PartnerRequestType
    {
        Franchise = 0,
        Revendeur = 1,
        Commissionnaire = 2
    }

    public enum PartnerRequestPersonType
    {
        Physique = 0,
        Morale = 1
    }

    public enum PartnerRequestStatus
    {
        Nouvelle = 0,
        EnCours = 1,
        Acceptee = 2,
        Refusee = 3
    }

    public class PartnerRequest
    {
        public int Id { get; set; }

        [Required]
        public PartnerRequestType PartnerType { get; set; } = PartnerRequestType.Franchise;

        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Company { get; set; }

        [Required, MaxLength(150)]
        public string City { get; set; } = string.Empty;

        [Required]
        public PartnerRequestPersonType PersonType { get; set; } = PartnerRequestPersonType.Physique;

        [Required, MaxLength(4000)]
        public string SelectedSolutionsJson { get; set; } = "[]";

        [Required]
        public PartnerRequestStatus Status { get; set; } = PartnerRequestStatus.Nouvelle;

        [MaxLength(500)]
        public string? InternalNote { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public IReadOnlyList<string> GetSelectedSolutions()
        {
            if (string.IsNullOrWhiteSpace(SelectedSolutionsJson))
            {
                return Array.Empty<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(SelectedSolutionsJson)
                    ?.Where(solution => !string.IsNullOrWhiteSpace(solution))
                    .Select(solution => solution.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()
                    ?? Array.Empty<string>();
            }
            catch (JsonException)
            {
                return Array.Empty<string>();
            }
        }

        public void SetSelectedSolutions(IEnumerable<string> solutions)
        {
            var normalized = solutions
                .Where(solution => !string.IsNullOrWhiteSpace(solution))
                .Select(solution => solution.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            SelectedSolutionsJson = JsonSerializer.Serialize(normalized);
        }
    }
}
