using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tunav_backend.Services;

namespace tunav_backend.Controllers
{
    // ══════════════════════════════════════════════════════════════════════════
    //  /api/events
    // ══════════════════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _svc;
        public EventsController(IEventService svc) => _svc = svc;

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status,
            [FromQuery] string? type,
            [FromQuery] bool? upcoming)
        {
            return Ok(await _svc.GetAllAsync(status, type, upcoming));
        }

        [HttpGet("{id:int}"), AllowAnonymous]
        public async Task<IActionResult> GetOne(int id)
        {
            var ev = await _svc.GetByIdAsync(id);
            return ev is null ? NotFound(new { message = "Événement introuvable." }) : Ok(ev);
        }

        [HttpPost, Authorize(Policy = "EventWrite")]
        public async Task<IActionResult> Create(
            [FromQuery] int userId,
            [FromBody] CreateEventRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Title))
                return BadRequest(new { message = "Le titre est obligatoire." });

            try
            {
                var dto = new CreateEventDto(
                    req.Title, req.Description, req.Type ?? "Autre",
                    req.StartDate, req.EndDate,
                    req.Location, req.OnlineLink, req.ParticipantCount,
                    req.CoverImageUrl, req.YoutubeUrl, req.ExternalUrl,
                    req.Publish);

                var ev = await _svc.CreateAsync(dto, userId);
                return CreatedAtAction(nameof(GetOne), new { id = ev.Id }, ev);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}"), Authorize(Policy = "EventWrite")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequest req)
        {
            try
            {
                var dto = new UpdateEventDto(
                    req.Title, req.Description, req.Type,
                    req.StartDate, req.EndDate,
                    req.Location, req.OnlineLink, req.ParticipantCount,
                    req.CoverImageUrl, req.YoutubeUrl, req.ExternalUrl,
                    req.Status);

                var ev = await _svc.UpdateAsync(id, dto);
                return ev is null ? NotFound(new { message = "Événement introuvable." }) : Ok(ev);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id:int}/toggle"), Authorize(Policy = "EventWrite")]
        public async Task<IActionResult> Toggle(int id)
        {
            var ev = await _svc.TogglePublishAsync(id);
            return ev is null ? NotFound(new { message = "Événement introuvable." }) : Ok(ev);
        }

        [HttpDelete("{id:int}"), Authorize(Policy = "EventWrite")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _svc.DeleteAsync(id);
            return ok ? NoContent() : NotFound(new { message = "Événement introuvable." });
        }

        [HttpPost("archive-expired"), Authorize(Policy = "EventWrite")]
        public async Task<IActionResult> ArchiveExpired()
        {
            var count = await _svc.ArchiveExpiredEventsAsync();
            return Ok(new { message = $"{count} événement(s) archivé(s).", count });
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  /api/collaborations  — toutes les demandes (backoffice + public)
    // ══════════════════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/collaborations")]
    public class CollaborationsController : ControllerBase
    {
        private readonly IEventService _svc;
        public CollaborationsController(IEventService svc) => _svc = svc;

        /// <summary>
        /// Liste des demandes (backoffice).
        /// Accès : Admin, Marketing, Ressources Humaines.
        /// Paramètre collaborationType filtre par onglet :
        ///   Collaboration | PropositionEvenement | DemandeFormation
        /// </summary>
        [HttpGet, Authorize(Policy = "CollaborationRead")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? eventId,
            [FromQuery] string? status,
            [FromQuery] string? collaborationType)
        {
            return Ok(await _svc.GetCollaborationsAsync(eventId, status, collaborationType));
        }

        [HttpGet("{id:int}"), Authorize(Policy = "CollaborationRead")]
        public async Task<IActionResult> GetOne(int id)
        {
            var r = await _svc.GetCollaborationAsync(id);
            return r is null ? NotFound(new { message = "Demande introuvable." }) : Ok(r);
        }

        /// <summary>
        /// Soumettre une demande de collaboration (public, sans authentification).
        /// Le champ CollaborationType dans le body détermine l'onglet de destination
        /// dans le backoffice :
        ///   "Collaboration"        → Pôle Formation form 1
        ///   "PropositionEvenement" → Formulaire page Événements
        ///   "DemandeFormation"     → Pôle Formation form 2
        /// </summary>
        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Submit([FromBody] SubmitCollaborationRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Organization))
                return BadRequest(new { message = "Le nom de l'organisme est obligatoire." });
            if (string.IsNullOrWhiteSpace(req.FullName))
                return BadRequest(new { message = "Le responsable est obligatoire." });
            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new { message = "L'email est obligatoire." });

            try
            {
                var dto = new SubmitCollaborationDto(
                    req.EventId,
                    req.Organization.Trim(), req.FullName.Trim(),
                    req.Phone, req.Email.Trim(),
                    req.Address, req.Message, req.AttachmentNames,
                    req.CollaborationType ?? "Collaboration",
                    req.IsHomologueMalek);

                var result = await _svc.SubmitCollaborationAsync(dto);

                return Ok(new
                {
                    message = "Votre demande a bien été envoyée. Nous vous contacterons prochainement.",
                    requestId = result.Id,
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id:int}/status"), Authorize(Policy = "CollaborationRead")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateCollaborationStatusRequest req)
        {
            try
            {
                var result = await _svc.UpdateCollaborationStatusAsync(id, req.Status, req.InternalNote);
                return result is null ? NotFound(new { message = "Demande introuvable." }) : Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}"), Authorize(Policy = "CollaborationRead")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _svc.DeleteCollaborationAsync(id);
            return ok ? NoContent() : NotFound(new { message = "Demande introuvable." });
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  /api/registrations — Inscriptions aux événements
    // ══════════════════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/registrations")]
    public class RegistrationsController : ControllerBase
    {
        private readonly IEventService _svc;
        public RegistrationsController(IEventService svc) => _svc = svc;

        [HttpGet, Authorize(Policy = "EventWrite")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? eventId,
            [FromQuery] string? status)
        {
            return Ok(await _svc.GetRegistrationsAsync(eventId, status));
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterEventRequest req)
        {
            // Organization est optionnelle pour les inscriptions individuelles
            if (string.IsNullOrWhiteSpace(req.FullName))
                return BadRequest(new { message = "Le nom complet est obligatoire." });
            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new { message = "L'email est obligatoire." });
            if (string.IsNullOrWhiteSpace(req.Phone))
                return BadRequest(new { message = "Le téléphone est obligatoire." });

            try
            {
                var dto = new RegisterForEventDto(
                    req.EventId, req.FullName.Trim(), req.Email.Trim(),
                    req.Phone, req.Organization, req.Message);

                var result = await _svc.RegisterForEventAsync(dto);
                return Ok(new
                {
                    message = "Votre inscription a bien été enregistrée. Nous vous contacterons prochainement.",
                    registrationId = result.Id,
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id:int}/status"), Authorize(Policy = "EventWrite")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateRegistrationStatusRequest req)
        {
            try
            {
                var result = await _svc.UpdateRegistrationStatusAsync(id, req.Status, req.InternalNote);
                return result is null ? NotFound(new { message = "Inscription introuvable." }) : Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}"), Authorize(Policy = "EventWrite")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _svc.DeleteRegistrationAsync(id);
            return ok ? NoContent() : NotFound(new { message = "Inscription introuvable." });
        }
    }

    // ── Request records ───────────────────────────────────────────────────────

    public record CreateEventRequest(
        string Title,
        string? Description,
        string? Type,
        DateTime StartDate,
        DateTime? EndDate,
        string? Location,
        string? OnlineLink,
        int? ParticipantCount,
        string? CoverImageUrl,
        string? YoutubeUrl,
        string? ExternalUrl,
        bool Publish = false);

    public record UpdateEventRequest(
        string? Title,
        string? Description,
        string? Type,
        DateTime? StartDate,
        DateTime? EndDate,
        string? Location,
        string? OnlineLink,
        int? ParticipantCount,
        string? CoverImageUrl,
        string? YoutubeUrl,
        string? ExternalUrl,
        string? Status);

    /// <summary>
    /// Utilisé par le formulaire Événements ET les deux formulaires Pôle Formation.
    /// CollaborationType (string) : "Collaboration" | "PropositionEvenement" | "DemandeFormation"
    /// IsHomologueMalek           : uniquement pour DemandeFormation
    /// </summary>
    public record SubmitCollaborationRequest(
        int? EventId,
        string Organization,
        string FullName,
        string? Phone,
        string Email,
        string? Address,
        string? Message,
        string? AttachmentNames,
        string? CollaborationType,      // détermine l'onglet backoffice
        bool? IsHomologueMalek = null); // spécifique au form 2 Pôle Formation

    public record UpdateCollaborationStatusRequest(
        string Status,
        string? InternalNote);

    public record RegisterEventRequest(
        int EventId,
        string FullName,
        string Email,
        string? Phone,
        string? Organization,
        string? Message);

    public record UpdateRegistrationStatusRequest(
        string Status,
        string? InternalNote);
}