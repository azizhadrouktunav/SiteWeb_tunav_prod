using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Services
{
    public record PartnerRequestDto(
        int Id,
        string PartnerType,
        string FullName,
        string Email,
        string Phone,
        string? Company,
        string City,
        string PersonType,
        string[] SelectedSolutions,
        string Status,
        string? InternalNote,
        DateTime SubmittedAt,
        DateTime? UpdatedAt);

    public record CreatePartnerRequestDto(
        string PartnerType,
        string FullName,
        string Email,
        string Phone,
        string? Company,
        string City,
        string PersonType,
        IReadOnlyCollection<string> SelectedSolutions);

    public interface IPartnerRequestService
    {
        Task<List<PartnerRequestDto>> GetAllAsync(string? status = null, string? partnerType = null);
        Task<PartnerRequestDto?> GetByIdAsync(int id);
        Task<PartnerRequestDto> CreateAsync(CreatePartnerRequestDto dto);
        Task<PartnerRequestDto?> UpdateStatusAsync(int id, string status, string? note);
        Task<bool> DeleteAsync(int id);
    }

    public class PartnerRequestService : IPartnerRequestService
    {
        private readonly AppDbContext _db;

        public PartnerRequestService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<PartnerRequestDto>> GetAllAsync(string? status = null, string? partnerType = null)
        {
            var query = _db.PartnerRequests.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<PartnerRequestStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(request => request.Status == parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(partnerType) &&
                Enum.TryParse<PartnerRequestType>(partnerType, true, out var parsedType))
            {
                query = query.Where(request => request.PartnerType == parsedType);
            }

            var items = await query
                .OrderByDescending(request => request.SubmittedAt)
                .ToListAsync();

            return items.Select(ToDto).ToList();
        }

        public async Task<PartnerRequestDto?> GetByIdAsync(int id)
        {
            var item = await _db.PartnerRequests.FindAsync(id);
            return item is null ? null : ToDto(item);
        }

        public async Task<PartnerRequestDto> CreateAsync(CreatePartnerRequestDto dto)
        {
            var request = new PartnerRequest
            {
                PartnerType = ParsePartnerType(dto.PartnerType),
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                Company = string.IsNullOrWhiteSpace(dto.Company) ? null : dto.Company.Trim(),
                City = dto.City.Trim(),
                PersonType = ParsePersonType(dto.PersonType),
                Status = PartnerRequestStatus.Nouvelle,
                SubmittedAt = DateTime.UtcNow
            };

            request.SetSelectedSolutions(dto.SelectedSolutions);

            if (request.GetSelectedSolutions().Count == 0)
            {
                throw new InvalidOperationException("Selectionnez au moins une solution.");
            }

            _db.PartnerRequests.Add(request);
            await _db.SaveChangesAsync();

            return ToDto(request);
        }

        public async Task<PartnerRequestDto?> UpdateStatusAsync(int id, string status, string? note)
        {
            if (!Enum.TryParse<PartnerRequestStatus>(status, true, out var parsedStatus))
            {
                throw new InvalidOperationException($"Statut invalide : '{status}'.");
            }

            var item = await _db.PartnerRequests.FindAsync(id);
            if (item is null)
            {
                return null;
            }

            item.Status = parsedStatus;
            item.InternalNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            item.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ToDto(item);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _db.PartnerRequests.FindAsync(id);
            if (item is null)
            {
                return false;
            }

            _db.PartnerRequests.Remove(item);
            await _db.SaveChangesAsync();
            return true;
        }

        private static PartnerRequestDto ToDto(PartnerRequest request) => new(
            request.Id,
            request.PartnerType.ToString(),
            request.FullName,
            request.Email,
            request.Phone,
            request.Company,
            request.City,
            request.PersonType.ToString(),
            request.GetSelectedSolutions().ToArray(),
            request.Status.ToString(),
            request.InternalNote,
            request.SubmittedAt,
            request.UpdatedAt);

        private static PartnerRequestType ParsePartnerType(string partnerType)
        {
            if (!Enum.TryParse<PartnerRequestType>(partnerType, true, out var parsedType))
            {
                throw new InvalidOperationException($"Type de partenariat invalide : '{partnerType}'.");
            }

            return parsedType;
        }

        private static PartnerRequestPersonType ParsePersonType(string personType)
        {
            if (!Enum.TryParse<PartnerRequestPersonType>(personType, true, out var parsedType))
            {
                throw new InvalidOperationException($"Type de personne invalide : '{personType}'.");
            }

            return parsedType;
        }
    }
}
