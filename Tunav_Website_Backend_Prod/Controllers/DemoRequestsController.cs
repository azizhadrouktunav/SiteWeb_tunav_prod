using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tunav_backend.Services;

namespace tunav_backend.Controllers
{
    [ApiController]
    [Route("api/demo-requests")]
    public class DemoRequestsController : ControllerBase
    {
        private readonly IDemoRequestService _svc;

        public DemoRequestsController(IDemoRequestService svc)
        {
            _svc = svc;
        }

        [HttpGet, Authorize(Policy = "CollaborationRead")]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status,
            [FromQuery] int? solutionId)
        {
            return Ok(await _svc.GetAllAsync(status, solutionId));
        }

        [HttpGet("{id:int}"), Authorize(Policy = "CollaborationRead")]
        public async Task<IActionResult> GetOne(int id)
        {
            var item = await _svc.GetByIdAsync(id);
            return item is null ? NotFound(new { message = "Demande de demo introuvable." }) : Ok(item);
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Submit([FromBody] SubmitDemoRequestRequest req)
        {
            if (req.SolutionId <= 0)
                return BadRequest(new { message = "La solution est obligatoire." });
            if (string.IsNullOrWhiteSpace(req.FirstName))
                return BadRequest(new { message = "Le prenom est obligatoire." });
            if (string.IsNullOrWhiteSpace(req.LastName))
                return BadRequest(new { message = "Le nom est obligatoire." });
            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new { message = "L email est obligatoire." });
            if (!IsValidEmail(req.Email))
                return BadRequest(new { message = "Veuillez saisir une adresse Gmail valide (ex: nom@gmail.com)." });
            if (string.IsNullOrWhiteSpace(req.Phone))
                return BadRequest(new { message = "Le numéro de téléphone est obligatoire." });
            if (req.HasWhatsapp is null)
                return BadRequest(new { message = "Veuillez preciser si vous avez un numero WhatsApp." });
            if (req.HasWhatsapp == true && string.IsNullOrWhiteSpace(req.WhatsappNumber))
                return BadRequest(new { message = "Le numero WhatsApp est obligatoire." });

            try
            {
                var result = await _svc.CreateAsync(new CreateDemoRequestDto(
                    req.SolutionId,
                    req.PackId,
                    req.FirstName,
                    req.LastName,
                    req.Email,
                    req.Phone,
                    req.HasWhatsapp.Value,
                    req.WhatsappNumber,
                    req.EntryPoint));

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
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateDemoRequestStatusRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Status))
                return BadRequest(new { message = "Le statut est obligatoire." });

            try
            {
                var result = await _svc.UpdateStatusAsync(id, req.Status, req.InternalNote);
                return result is null ? NotFound(new { message = "Demande de demo introuvable." }) : Ok(result);
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
            return deleted ? NoContent() : NotFound(new { message = "Demande de demo introuvable." });
        }

        private static bool IsValidEmail(string email)
        {
            var normalized = email.Trim();
            return System.Text.RegularExpressions.Regex.IsMatch(
                normalized,
                @"^[^\s@]+@gmail\.com$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }

    public record SubmitDemoRequestRequest(
        int SolutionId,
        int? PackId,
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        bool? HasWhatsapp,
        string? WhatsappNumber,
        string? EntryPoint = null);

    public record UpdateDemoRequestStatusRequest(
        string Status,
        string? InternalNote);
}


