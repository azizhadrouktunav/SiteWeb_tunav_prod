using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using tunav_backend.Services;

namespace tunav_backend.Controllers
{
    [ApiController]
    [Route("api/partner-requests")]
    public class PartnerRequestsController : ControllerBase
    {
        private readonly IPartnerRequestService _svc;

        public PartnerRequestsController(IPartnerRequestService svc)
        {
            _svc = svc;
        }

        [HttpGet, Authorize(Policy = "CollaborationRead")]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status,
            [FromQuery] string? partnerType)
        {
            return Ok(await _svc.GetAllAsync(status, partnerType));
        }

        [HttpGet("{id:int}"), Authorize(Policy = "CollaborationRead")]
        public async Task<IActionResult> GetOne(int id)
        {
            var item = await _svc.GetByIdAsync(id);
            return item is null ? NotFound(new { message = "Demande partenaire introuvable." }) : Ok(item);
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Submit([FromBody] SubmitPartnerRequestRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.PartnerType))
                return BadRequest(new { message = "Le type de partenariat est obligatoire." });
            if (string.IsNullOrWhiteSpace(req.FullName))
                return BadRequest(new { message = "Le nom complet est obligatoire." });
            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new { message = "L'email est obligatoire." });
            if (!IsValidEmail(req.Email))
                return BadRequest(new { message = "Veuillez saisir une adresse e-mail valide." });
            if (string.IsNullOrWhiteSpace(req.Phone))
                return BadRequest(new { message = "Le numero de telephone est obligatoire." });
            if (string.IsNullOrWhiteSpace(req.City))
                return BadRequest(new { message = "La ville est obligatoire." });
            if (string.IsNullOrWhiteSpace(req.PersonType))
                return BadRequest(new { message = "Le type de personne est obligatoire." });
            if (req.SelectedSolutions is null ||
                !req.SelectedSolutions.Any(solution => !string.IsNullOrWhiteSpace(solution)))
                return BadRequest(new { message = "Selectionnez au moins une solution." });

            try
            {
                var result = await _svc.CreateAsync(new CreatePartnerRequestDto(
                    req.PartnerType,
                    req.FullName,
                    req.Email,
                    req.Phone,
                    req.Company,
                    req.City,
                    req.PersonType,
                    req.SelectedSolutions));

                return Ok(new
                {
                    message = "Votre demande a bien ete recue et sera examinee par notre equipe.",
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
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdatePartnerRequestStatusRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Status))
                return BadRequest(new { message = "Le statut est obligatoire." });

            try
            {
                var result = await _svc.UpdateStatusAsync(id, req.Status, req.InternalNote);
                return result is null ? NotFound(new { message = "Demande partenaire introuvable." }) : Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}"), Authorize(Policy = "CollaborationRead")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _svc.DeleteAsync(id);
            return deleted ? NoContent() : NotFound(new { message = "Demande partenaire introuvable." });
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var normalized = email.Trim();
                var mail = new MailAddress(normalized);
                return mail.Address.Equals(normalized, StringComparison.OrdinalIgnoreCase);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }

    public record SubmitPartnerRequestRequest(
        string PartnerType,
        string FullName,
        string Email,
        string Phone,
        string? Company,
        string City,
        string PersonType,
        string[] SelectedSolutions);

    public record UpdatePartnerRequestStatusRequest(
        string Status,
        string? InternalNote);
}
