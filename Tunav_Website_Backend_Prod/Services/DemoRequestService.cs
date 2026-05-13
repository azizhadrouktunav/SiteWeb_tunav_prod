using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Services
{
    public record DemoRequestDto(
        int Id,
        int SolutionId,
        string SolutionTitle,
        int? PackId,
        string? PackName,
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        bool HasWhatsapp,
        string? WhatsappNumber,
        string EntryPoint,
        string Status,
        string? InternalNote,
        DateTime SubmittedAt,
        DateTime? UpdatedAt);

    public record CreateDemoRequestDto(
        int SolutionId,
        int? PackId,
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        bool HasWhatsapp,
        string? WhatsappNumber,
        string? EntryPoint);

    public interface IDemoRequestService
    {
        Task<List<DemoRequestDto>> GetAllAsync(string? status = null, int? solutionId = null);
        Task<DemoRequestDto?> GetByIdAsync(int id);
        Task<DemoRequestDto> CreateAsync(CreateDemoRequestDto dto);
        Task<DemoRequestDto?> UpdateStatusAsync(int id, string status, string? note);
        Task<bool> DeleteAsync(int id);
    }

    public class DemoRequestService : IDemoRequestService
    {
        private readonly AppDbContext _db;

        public DemoRequestService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<DemoRequestDto>> GetAllAsync(string? status = null, int? solutionId = null)
        {
            var query = _db.DemoRequests.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<DemoRequestStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(request => request.Status == parsedStatus);
            }

            if (solutionId.HasValue && solutionId.Value > 0)
            {
                query = query.Where(request => request.SolutionId == solutionId.Value);
            }

            var items = await query
                .OrderByDescending(request => request.SubmittedAt)
                .ToListAsync();

            return items.Select(ToDto).ToList();
        }

        public async Task<DemoRequestDto?> GetByIdAsync(int id)
        {
            var item = await _db.DemoRequests.FindAsync(id);
            return item is null ? null : ToDto(item);
        }

        public async Task<DemoRequestDto> CreateAsync(CreateDemoRequestDto dto)
        {
            var solution = await _db.Solutions
                .AsNoTracking()
                .Include(item => item.BaseSolution)
                .FirstOrDefaultAsync(item => item.Id == dto.SolutionId && item.IsActive);

            if (solution is null)
            {
                throw new InvalidOperationException("La solution selectionnee est invalide ou inactive.");
            }

            Pack? pack = null;
            if (dto.PackId.HasValue && dto.PackId.Value > 0)
            {
                var effectivePackSolutionId = solution.Type == SolutionType.General
                    ? solution.Id
                    : solution.BaseSolutionId;

                if (!effectivePackSolutionId.HasValue || effectivePackSolutionId.Value <= 0)
                {
                    throw new InvalidOperationException("La solution selectionnee ne reference aucun pack actif.");
                }

                pack = await _db.Packs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item =>
                        item.Id == dto.PackId.Value &&
                        item.SolutionId == effectivePackSolutionId.Value &&
                        item.IsActive);

                if (pack is null)
                {
                    throw new InvalidOperationException("Le pack selectionne est invalide ou inactif.");
                }
            }

            var request = new DemoRequest
            {
                SolutionId = solution.Id,
                SolutionTitle = solution.Title.Trim(),
                PackId = pack?.Id,
                PackName = pack?.Name,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                HasWhatsapp = dto.HasWhatsapp,
                WhatsappNumber = dto.HasWhatsapp
                    ? dto.WhatsappNumber?.Trim()
                    : null,
                EntryPoint = ParseEntryPoint(dto.EntryPoint),
                Status = DemoRequestStatus.Nouvelle,
                SubmittedAt = DateTime.UtcNow
            };

            _db.DemoRequests.Add(request);
            await _db.SaveChangesAsync();

            return ToDto(request);
        }

        public async Task<DemoRequestDto?> UpdateStatusAsync(int id, string status, string? note)
        {
            if (!Enum.TryParse<DemoRequestStatus>(status, true, out var parsedStatus))
            {
                throw new InvalidOperationException($"Statut invalide : '{status}'.");
            }

            var item = await _db.DemoRequests.FindAsync(id);
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
            var item = await _db.DemoRequests.FindAsync(id);
            if (item is null)
            {
                return false;
            }

            _db.DemoRequests.Remove(item);
            await _db.SaveChangesAsync();
            return true;
        }

        private static DemoRequestDto ToDto(DemoRequest request) => new(
            request.Id,
            request.SolutionId,
            request.SolutionTitle,
            request.PackId,
            request.PackName,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.HasWhatsapp,
            request.WhatsappNumber,
            request.EntryPoint.ToString(),
            request.Status.ToString(),
            request.InternalNote,
            request.SubmittedAt,
            request.UpdatedAt);

        private static DemoRequestEntryPoint ParseEntryPoint(string? entryPoint)
        {
            if (string.IsNullOrWhiteSpace(entryPoint))
            {
                return DemoRequestEntryPoint.DemoPage;
            }

            if (!Enum.TryParse<DemoRequestEntryPoint>(entryPoint, true, out var parsedEntryPoint))
            {
                throw new InvalidOperationException($"Point d entree invalide : '{entryPoint}'.");
            }

            return parsedEntryPoint;
        }
    }
}