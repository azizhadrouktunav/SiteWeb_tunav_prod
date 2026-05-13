using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Services
{
    // ── DTOs ─────────────────────────────────────────────────────────────────

    public record EventDto(
        int Id, string Title, string? Description,
        string Type, string Status,
        DateTime StartDate, DateTime? EndDate,
        string? Location, string? OnlineLink, int? ParticipantCount,
        string? CoverImageUrl, string? YoutubeUrl, string? YoutubeEmbedId,
        string? ExternalUrl,
        bool IsArchived, bool IsUpcoming,
        int CreatedBy, string CreatedByName,
        int CollaborationCount,
        int RegistrationCount,      // ← AJOUT : nombre d'inscriptions
        DateTime CreatedAt, DateTime? UpdatedAt);

    public record CollaborationRequestDto(
        int Id, int? EventId, string EventTitle,
        string CollaborationType,
        string Organization, string FullName,
        string? Phone, string Email,
        string? Address, string? Message, string? AttachmentNames,
        bool? IsHomologueMalek,
        string Status, string? InternalNote,
        DateTime SubmittedAt, DateTime? UpdatedAt);

    // ── Interface ────────────────────────────────────────────────────────────

    public interface IEventService
    {
        // Événements
        Task<List<EventDto>> GetAllAsync(string? status = null, string? type = null, bool? upcoming = null);
        Task<EventDto?> GetByIdAsync(int id);
        Task<EventDto> CreateAsync(CreateEventDto dto, int userId);
        Task<EventDto?> UpdateAsync(int id, UpdateEventDto dto);
        Task<EventDto?> TogglePublishAsync(int id);
        Task<bool> DeleteAsync(int id);
        Task<int> ArchiveExpiredEventsAsync();

        // Demandes de collaboration (filtrables par CollaborationType)
        Task<List<CollaborationRequestDto>> GetCollaborationsAsync(int? eventId = null, string? status = null, string? collaborationType = null);
        Task<CollaborationRequestDto?> GetCollaborationAsync(int id);
        Task<CollaborationRequestDto> SubmitCollaborationAsync(SubmitCollaborationDto dto);
        Task<CollaborationRequestDto?> UpdateCollaborationStatusAsync(int id, string status, string? note);
        Task<bool> DeleteCollaborationAsync(int id);

        // Inscriptions événements
        Task<List<EventRegistrationDto>> GetRegistrationsAsync(int? eventId = null, string? status = null);
        Task<EventRegistrationDto> RegisterForEventAsync(RegisterForEventDto dto);
        Task<EventRegistrationDto?> UpdateRegistrationStatusAsync(int id, string status, string? note);
        Task<bool> DeleteRegistrationAsync(int id);
    }

    // ── DTOs d'entrée ────────────────────────────────────────────────────────

    public record CreateEventDto(
        string Title, string? Description, string Type,
        DateTime StartDate, DateTime? EndDate,
        string? Location, string? OnlineLink, int? ParticipantCount,
        string? CoverImageUrl, string? YoutubeUrl, string? ExternalUrl,
        bool Publish = false);

    public record UpdateEventDto(
        string? Title, string? Description, string? Type,
        DateTime? StartDate, DateTime? EndDate,
        string? Location, string? OnlineLink, int? ParticipantCount,
        string? CoverImageUrl, string? YoutubeUrl, string? ExternalUrl,
        string? Status);

    public record EventRegistrationDto(
        int Id, int EventId, string EventTitle,
        string FullName, string Email,
        string? Phone, string? Organization, string? Message,
        string Status, string? InternalNote,
        DateTime RegisteredAt, DateTime? UpdatedAt);

    public record RegisterForEventDto(
        int EventId, string FullName, string Email,
        string? Phone, string? Organization, string? Message);

    /// <summary>
    /// DTO commun pour toutes les soumissions de collaboration.
    /// CollaborationType détermine l'onglet backoffice de destination :
    ///   "Collaboration"        → Pôle Formation form 1
    ///   "PropositionEvenement" → Page Événements
    ///   "DemandeFormation"     → Pôle Formation form 2
    /// </summary>
    public record SubmitCollaborationDto(
        int? EventId,
        string Organization, string FullName,
        string? Phone, string Email,
        string? Address, string? Message, string? AttachmentNames,
        string CollaborationType = "Collaboration",
        bool? IsHomologueMalek = null);

    // ── Implémentation ────────────────────────────────────────────────────────

    public class EventService : IEventService
    {
        private readonly AppDbContext _db;
        private readonly INotificationService _notify;

        public EventService(AppDbContext db, INotificationService notify)
        {
            _db = db;
            _notify = notify;
        }

        // ── Mapping ──────────────────────────────────────────────────────────

        // MODIFIÉ : ajout du paramètre regCount pour RegistrationCount
        private static EventDto ToDto(Event e, int colabCount = 0, int regCount = 0) => new(
            e.Id, e.Title, e.Description,
            e.Type.ToString(), e.Status.ToString(),
            e.StartDate, e.EndDate,
            e.Location, e.OnlineLink, e.ParticipantCount,
            e.CoverImageUrl, e.YoutubeUrl, e.YoutubeEmbedId,
            e.ExternalUrl,
            e.IsArchived, e.IsUpcoming,
            e.CreatedBy, e.CreatedByName,
            colabCount,
            regCount,       // ← AJOUT
            e.CreatedAt, e.UpdatedAt);

        private static CollaborationRequestDto ToDto(CollaborationRequest r) => new(
            r.Id, r.EventId, r.EventTitle,
            r.CollaborationType.ToString(),
            r.Organization, r.FullName,
            r.Phone, r.Email,
            r.Address, r.Message, r.AttachmentNames,
            r.IsHomologueMalek,
            r.Status.ToString(), r.InternalNote,
            r.SubmittedAt, r.UpdatedAt);

        // MODIFIÉ : inclure aussi les Registrations
        private IQueryable<Event> EventsQuery() =>
            _db.Events
                .Include(e => e.CreatedByUser)
                .Include(e => e.CollaborationRequests)
                .Include(e => e.Registrations);   // ← AJOUT

        // ── ÉVÉNEMENTS ───────────────────────────────────────────────────────

        public async Task<List<EventDto>> GetAllAsync(
            string? status = null, string? type = null, bool? upcoming = null)
        {
            await ArchiveExpiredEventsAsync();

            var q = EventsQuery();

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<EventStatus>(status, true, out var s))
                q = q.Where(e => e.Status == s);

            if (!string.IsNullOrWhiteSpace(type) &&
                Enum.TryParse<EventType>(type, true, out var t))
                q = q.Where(e => e.Type == t);

            if (upcoming == true)
                q = q.Where(e => e.StartDate > DateTime.UtcNow && e.Status == EventStatus.Published);

            return await q
                .OrderBy(e => e.StartDate)
                // MODIFIÉ : passer aussi e.Registrations.Count
                .Select(e => ToDto(e, e.CollaborationRequests.Count, e.Registrations.Count))
                .ToListAsync();
        }

        public async Task<EventDto?> GetByIdAsync(int id)
        {
            var e = await EventsQuery().FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return null;
            // MODIFIÉ : passer aussi e.Registrations.Count
            return ToDto(e, e.CollaborationRequests.Count, e.Registrations.Count);
        }

        public async Task<EventDto> CreateAsync(CreateEventDto dto, int userId)
        {
            if (!Enum.TryParse<EventType>(dto.Type, true, out var type))
                throw new InvalidOperationException($"Type invalide : '{dto.Type}'.");

            var ev = new Event
            {
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                Type = type,
                Status = dto.Publish ? EventStatus.Published : EventStatus.Draft,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Location = dto.Location?.Trim(),
                OnlineLink = dto.OnlineLink?.Trim(),
                ParticipantCount = dto.ParticipantCount,
                CoverImageUrl = dto.CoverImageUrl?.Trim(),
                YoutubeUrl = dto.YoutubeUrl?.Trim(),
                ExternalUrl = dto.ExternalUrl?.Trim(),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();
            await _db.Entry(ev).Reference(e => e.CreatedByUser).LoadAsync();
            // Nouvel événement → 0 collaborations, 0 inscriptions
            return ToDto(ev, 0, 0);
        }

        public async Task<EventDto?> UpdateAsync(int id, UpdateEventDto dto)
        {
            var ev = await EventsQuery().FirstOrDefaultAsync(e => e.Id == id);
            if (ev is null) return null;

            if (!string.IsNullOrWhiteSpace(dto.Title)) ev.Title = dto.Title.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Description)) ev.Description = dto.Description.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Type) &&
                Enum.TryParse<EventType>(dto.Type, true, out var t)) ev.Type = t;
            if (!string.IsNullOrWhiteSpace(dto.Status) &&
                Enum.TryParse<EventStatus>(dto.Status, true, out var s)) ev.Status = s;
            if (dto.StartDate.HasValue) ev.StartDate = dto.StartDate.Value;
            if (dto.EndDate.HasValue) ev.EndDate = dto.EndDate;
            if (dto.Location is not null) ev.Location = dto.Location.Trim();
            if (dto.OnlineLink is not null) ev.OnlineLink = dto.OnlineLink.Trim();
            if (dto.ParticipantCount.HasValue) ev.ParticipantCount = dto.ParticipantCount;
            if (dto.CoverImageUrl is not null) ev.CoverImageUrl = dto.CoverImageUrl.Trim();
            if (dto.YoutubeUrl is not null) ev.YoutubeUrl = dto.YoutubeUrl.Trim();
            if (dto.ExternalUrl is not null) ev.ExternalUrl = dto.ExternalUrl.Trim();
            ev.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            // MODIFIÉ : passer aussi e.Registrations.Count
            return ToDto(ev, ev.CollaborationRequests.Count, ev.Registrations.Count);
        }

        public async Task<EventDto?> TogglePublishAsync(int id)
        {
            var ev = await EventsQuery().FirstOrDefaultAsync(e => e.Id == id);
            if (ev is null) return null;

            ev.Status = ev.Status == EventStatus.Published
                ? EventStatus.Draft
                : EventStatus.Published;
            ev.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            // MODIFIÉ : passer aussi e.Registrations.Count
            return ToDto(ev, ev.CollaborationRequests.Count, ev.Registrations.Count);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var ev = await _db.Events.FindAsync(id);
            if (ev is null) return false;
            _db.Events.Remove(ev);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<int> ArchiveExpiredEventsAsync()
        {
            var expired = await _db.Events
                .Where(e => e.StartDate < DateTime.UtcNow
                         && e.Status == EventStatus.Published
                         && !e.IsArchived)
                .ToListAsync();

            foreach (var ev in expired)
            {
                ev.Status = EventStatus.Archived;
                ev.IsArchived = true;
                ev.UpdatedAt = DateTime.UtcNow;
            }

            if (expired.Count > 0)
                await _db.SaveChangesAsync();

            return expired.Count;
        }

        // ── DEMANDES DE COLLABORATION ────────────────────────────────────────

        public async Task<List<CollaborationRequestDto>> GetCollaborationsAsync(
            int? eventId = null, string? status = null, string? collaborationType = null)
        {
            var q = _db.CollaborationRequests
                .Include(r => r.Event)
                .AsQueryable();

            if (eventId.HasValue)
                q = q.Where(r => r.EventId == eventId.Value);

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<CollaborationStatus>(status, true, out var cs))
                q = q.Where(r => r.Status == cs);

            // Filtrer par type (onglet backoffice)
            if (!string.IsNullOrWhiteSpace(collaborationType) &&
                Enum.TryParse<CollaborationType>(collaborationType, true, out var ct))
                q = q.Where(r => r.CollaborationType == ct);

            return await q
                .OrderByDescending(r => r.SubmittedAt)
                .Select(r => ToDto(r))
                .ToListAsync();
        }

        public async Task<CollaborationRequestDto?> GetCollaborationAsync(int id)
        {
            var r = await _db.CollaborationRequests
                .Include(x => x.Event)
                .FirstOrDefaultAsync(x => x.Id == id);
            return r is null ? null : ToDto(r);
        }

        public async Task<CollaborationRequestDto> SubmitCollaborationAsync(SubmitCollaborationDto dto)
        {
            // Résoudre le CollaborationType
            if (!Enum.TryParse<CollaborationType>(dto.CollaborationType, true, out var ct))
                ct = CollaborationType.Collaboration;

            // Vérifier l'événement seulement pour les propositions liées à un événement
            string eventTitle = "—";
            if (dto.EventId.HasValue)
            {
                var ev = await _db.Events.FindAsync(dto.EventId.Value);
                if (ev is null || ev.Status != EventStatus.Published)
                    throw new InvalidOperationException("Événement introuvable ou non publié.");
                eventTitle = ev.Title;
            }

            var req = new CollaborationRequest
            {
                EventId = dto.EventId,
                CollaborationType = ct,
                Organization = dto.Organization.Trim(),
                FullName = dto.FullName.Trim(),
                Phone = dto.Phone?.Trim(),
                Email = dto.Email.Trim(),
                Address = dto.Address?.Trim(),
                Message = dto.Message?.Trim(),
                AttachmentNames = dto.AttachmentNames?.Trim(),
                IsHomologueMalek = dto.IsHomologueMalek,
                Status = CollaborationStatus.Nouvelle,
                SubmittedAt = DateTime.UtcNow
            };

            _db.CollaborationRequests.Add(req);
            await _db.SaveChangesAsync();
            if (req.EventId.HasValue)
                await _db.Entry(req).Reference(r => r.Event).LoadAsync();

            await _notify.NotifyNewCollaborationAsync(req, eventTitle);

            return ToDto(req);
        }

        public async Task<CollaborationRequestDto?> UpdateCollaborationStatusAsync(
            int id, string status, string? note)
        {
            if (!Enum.TryParse<CollaborationStatus>(status, true, out var cs))
                throw new InvalidOperationException($"Statut invalide : '{status}'.");

            var req = await _db.CollaborationRequests
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (req is null) return null;

            req.Status = cs;
            req.InternalNote = note?.Trim();
            req.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return ToDto(req);
        }

        public async Task<bool> DeleteCollaborationAsync(int id)
        {
            var req = await _db.CollaborationRequests.FindAsync(id);
            if (req is null) return false;
            _db.CollaborationRequests.Remove(req);
            await _db.SaveChangesAsync();
            return true;
        }

        // ── INSCRIPTIONS ─────────────────────────────────────────────────────

        private static EventRegistrationDto ToRegDto(EventRegistration r) => new(
            r.Id, r.EventId, r.EventTitle,
            r.FullName, r.Email,
            r.Phone, r.Organization, r.Message,
            r.Status.ToString(), r.InternalNote,
            r.RegisteredAt, r.UpdatedAt);

        public async Task<List<EventRegistrationDto>> GetRegistrationsAsync(
            int? eventId = null, string? status = null)
        {
            var q = _db.EventRegistrations.Include(r => r.Event).AsQueryable();
            if (eventId.HasValue) q = q.Where(r => r.EventId == eventId.Value);
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<RegistrationStatus>(status, true, out var rs))
                q = q.Where(r => r.Status == rs);
            return await q.OrderByDescending(r => r.RegisteredAt)
                          .Select(r => ToRegDto(r)).ToListAsync();
        }

        public async Task<EventRegistrationDto> RegisterForEventAsync(RegisterForEventDto dto)
        {
            var ev = await _db.Events.FindAsync(dto.EventId);
            if (ev is null || ev.Status != EventStatus.Published)
                throw new InvalidOperationException("Événement introuvable ou non publié.");

            var reg = new EventRegistration
            {
                EventId = dto.EventId,
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone?.Trim(),
                Organization = dto.Organization?.Trim(),
                Message = dto.Message?.Trim(),
                Status = RegistrationStatus.Nouvelle,
                RegisteredAt = DateTime.UtcNow
            };

            _db.EventRegistrations.Add(reg);
            await _db.SaveChangesAsync();
            await _db.Entry(reg).Reference(r => r.Event).LoadAsync();
            return ToRegDto(reg);
        }

        public async Task<EventRegistrationDto?> UpdateRegistrationStatusAsync(
            int id, string status, string? note)
        {
            if (!Enum.TryParse<RegistrationStatus>(status, true, out var rs))
                throw new InvalidOperationException($"Statut invalide : '{status}'.");
            var reg = await _db.EventRegistrations.Include(r => r.Event)
                               .FirstOrDefaultAsync(r => r.Id == id);
            if (reg is null) return null;
            reg.Status = rs;
            reg.InternalNote = note?.Trim();
            reg.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return ToRegDto(reg);
        }

        public async Task<bool> DeleteRegistrationAsync(int id)
        {
            var reg = await _db.EventRegistrations.FindAsync(id);
            if (reg is null) return false;
            _db.EventRegistrations.Remove(reg);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}