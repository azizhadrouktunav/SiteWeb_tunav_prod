using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;
using tunav_backend.Services;

namespace tunav_backend.Controllers;

// ─────────────────────────────────────────────────────────────────────────────
//  /api/job-offers        — Offres d'emploi / stage
//  /api/job-applications  — Candidatures
// ─────────────────────────────────────────────────────────────────────────────

// ── Controller principal : routes publiques + CRUD ────────────────────────────
[ApiController]
[Route("api/job-offers")]
public class JobOffersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public JobOffersController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ── Lecture publique (site) ───────────────────────────────────────────────

    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> GetOffers(
        [FromQuery] string? contractType,
        [FromQuery] string? academicLevel,
        [FromQuery] string? postType,
        [FromQuery] bool? isActive)
    {
        var q = _db.JobOffers
            .Include(o => o.CreatedByUser)
            .Where(o => !o.IsArchived)
            .AsQueryable();

        if (isActive.HasValue)
            q = q.Where(o => o.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(contractType) &&
            Enum.TryParse<JobContractType>(contractType, true, out var ct))
            q = q.Where(o => o.ContractType == ct);

        if (!string.IsNullOrWhiteSpace(academicLevel) &&
            Enum.TryParse<JobAcademicLevel>(academicLevel, true, out var al))
            q = q.Where(o => o.AcademicLevel == al);

        if (!string.IsNullOrWhiteSpace(postType) &&
            Enum.TryParse<JobPostType>(postType, true, out var pt))
            q = q.Where(o => o.PostType == pt);

        var list = await q.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(list.Select(o => ToDto(o)));
    }

    [HttpGet("{id:int}"), AllowAnonymous]
    public async Task<IActionResult> GetOffer(int id)
    {
        var o = await _db.JobOffers.Include(x => x.CreatedByUser)
                         .FirstOrDefaultAsync(x => x.Id == id);
        return o is null ? NotFound(new { message = "Offre introuvable." }) : Ok(ToDto(o));
    }

    // ── Backoffice : toutes les offres y compris archivées ────────────────────
    [HttpGet("all"), Authorize(Policy = "HrWrite")]
    public async Task<IActionResult> GetAllOffers([FromQuery] bool? isArchived)
    {
        var q = _db.JobOffers.Include(o => o.CreatedByUser).AsQueryable();
        if (isArchived.HasValue)
            q = q.Where(o => o.IsArchived == isArchived.Value);

        var list = await q.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(list.Select(o => ToDto(o, includeAppCount: true)));
    }

    // ── CRUD backoffice ───────────────────────────────────────────────────────

    [HttpPost, Authorize(Policy = "HrWrite")]
    public async Task<IActionResult> CreateOffer([FromBody] JobOfferRequest req, [FromQuery] int userId)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { message = "Le titre est obligatoire." });

        if (!Enum.TryParse<JobContractType>(req.ContractType, true, out var ct))
            return BadRequest(new { message = $"ContractType invalide : {req.ContractType}" });
        if (!Enum.TryParse<JobAcademicLevel>(req.AcademicLevel, true, out var al))
            return BadRequest(new { message = $"AcademicLevel invalide : {req.AcademicLevel}" });
        if (!Enum.TryParse<JobPostType>(req.PostType, true, out var pt))
            return BadRequest(new { message = $"PostType invalide : {req.PostType}" });

        var offer = new JobOffer
        {
            Title = req.Title.Trim(),
            Description = req.Description?.Trim(),
            Missions = req.Missions?.Trim(),
            Benefits = req.Benefits?.Trim(),
            Process = req.Process?.Trim(),
            Requirements = req.Requirements?.Trim(),
            ContractType = ct,
            AcademicLevel = al,
            PostType = pt,
            Location = req.Location?.Trim(),
            Duration = req.Duration?.Trim(),
            Salary = req.Salary?.Trim(),
            Skills = NormalizeSkills(req.Skills),
            Deadline = req.Deadline.HasValue
                ? DateTime.SpecifyKind(req.Deadline.Value, DateTimeKind.Utc) : null,
            IsActive = req.IsActive ?? true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.JobOffers.Add(offer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetOffer), new { id = offer.Id }, ToDto(offer));
    }

    [HttpPut("{id:int}"), Authorize(Policy = "HrWrite")]
    public async Task<IActionResult> UpdateOffer(int id, [FromBody] JobOfferRequest req)
    {
        var offer = await _db.JobOffers.FindAsync(id);
        if (offer is null) return NotFound(new { message = "Offre introuvable." });

        if (!Enum.TryParse<JobContractType>(req.ContractType, true, out var ct))
            return BadRequest(new { message = "ContractType invalide." });
        if (!Enum.TryParse<JobAcademicLevel>(req.AcademicLevel, true, out var al))
            return BadRequest(new { message = "AcademicLevel invalide." });
        if (!Enum.TryParse<JobPostType>(req.PostType, true, out var pt))
            return BadRequest(new { message = "PostType invalide." });

        offer.Title = req.Title?.Trim() ?? offer.Title;
        offer.Description = req.Description?.Trim() ?? offer.Description;
        offer.Missions = req.Missions?.Trim() ?? offer.Missions;
        offer.Benefits = req.Benefits?.Trim() ?? offer.Benefits;
        offer.Process = req.Process?.Trim() ?? offer.Process;
        offer.Requirements = req.Requirements?.Trim() ?? offer.Requirements;
        offer.ContractType = ct;
        offer.AcademicLevel = al;
        offer.PostType = pt;
        offer.Location = req.Location?.Trim() ?? offer.Location;
        offer.Duration = req.Duration?.Trim() ?? offer.Duration;
        offer.Salary = req.Salary?.Trim() ?? offer.Salary;
        offer.Skills = NormalizeSkills(req.Skills) ?? offer.Skills;
        offer.Deadline = req.Deadline.HasValue
            ? DateTime.SpecifyKind(req.Deadline.Value, DateTimeKind.Utc) : offer.Deadline;
        offer.IsActive = req.IsActive ?? offer.IsActive;
        offer.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToDto(offer));
    }

    [HttpPatch("{id:int}/toggle"), Authorize(Policy = "HrWrite")]
    public async Task<IActionResult> ToggleOffer(int id)
    {
        var offer = await _db.JobOffers.FindAsync(id);
        if (offer is null) return NotFound(new { message = "Offre introuvable." });
        offer.IsActive = !offer.IsActive;
        offer.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(ToDto(offer));
    }

    [HttpDelete("{id:int}"), Authorize(Policy = "HrWrite")]
    public async Task<IActionResult> DeleteOffer(int id)
    {
        var offer = await _db.JobOffers.FindAsync(id);
        if (offer is null) return NotFound(new { message = "Offre introuvable." });
        _db.JobOffers.Remove(offer);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("archive-expired"), Authorize(Policy = "HrWrite")]
    public async Task<IActionResult> ArchiveExpired()
    {
        var expired = await _db.JobOffers
            .Where(o => o.Deadline.HasValue && o.Deadline.Value < DateTime.UtcNow && !o.IsArchived)
            .ToListAsync();

        foreach (var o in expired)
        {
            o.IsArchived = true;
            o.IsActive = false;
            o.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return Ok(new { message = $"{expired.Count} offre(s) archivée(s).", count = expired.Count });
    }

    /// <summary>
    /// Normalise les compétences : si l'admin saisit sans virgule,
    /// on détecte les mots capitalisés pour les séparer automatiquement.
    /// Sinon on nettoie juste les espaces autour des virgules.
    /// </summary>
    private static string? NormalizeSkills(string? skills)
    {
        if (string.IsNullOrWhiteSpace(skills)) return null;
        skills = skills.Trim();

        if (skills.Contains(','))
            return string.Join(", ", skills.Split(',')
                .Select(s => s.Trim()).Where(s => s.Length > 0));

        if (skills.Contains(';'))
            return string.Join(", ", skills.Split(';')
                .Select(s => s.Trim()).Where(s => s.Length > 0));

        return skills;
    }

    private static object ToDto(JobOffer o, bool includeAppCount = false) => new
    {
        o.Id,
        o.Title,
        o.Description,
        o.Missions,
        o.Benefits,
        o.Process,
        o.Requirements,
        contractType = o.ContractType.ToString(),
        academicLevel = o.AcademicLevel.ToString(),
        postType = o.PostType.ToString(),
        status = o.Status.ToString(),
        o.Location,
        o.Duration,
        o.Salary,
        o.Skills,
        skillList = o.SkillList,
        o.Deadline,
        o.IsActive,
        o.IsArchived,
        o.IsExpired,
        o.CreatedBy,
        createdByName = o.CreatedByName,
        o.CreatedAt,
        o.UpdatedAt,
        applicationCount = includeAppCount ? (int?)o.Applications.Count : null
    };
}

// ── Controller candidatures ────────────────────────────────────────────────────
[ApiController]
[Route("api/job-applications")]
public class JobApplicationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public JobApplicationsController(
        AppDbContext db,
        IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpPost, AllowAnonymous]
    [RequestSizeLimit(15_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Apply([FromForm] JobApplicationFormRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FirstName))
            return BadRequest(new { message = "Le prénom est obligatoire." });
        if (string.IsNullOrWhiteSpace(req.LastName))
            return BadRequest(new { message = "Le nom est obligatoire." });
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { message = "L'email est obligatoire." });

        var offer = await _db.JobOffers.FindAsync(req.JobOfferId);
        if (offer is null || !offer.IsActive || offer.IsArchived)
            return BadRequest(new { message = "Cette offre n'est plus disponible." });

        var uploadsDir = Path.Combine(_env.ContentRootPath, "Uploads", "applications");
        Directory.CreateDirectory(uploadsDir);

        string? cvFile = null;
        string? motivationFile = null;

        if (req.CvFile != null && req.CvFile.Length > 0)
        {
            var ext = Path.GetExtension(req.CvFile.FileName).ToLowerInvariant();
            if (!new[] { ".pdf", ".doc", ".docx" }.Contains(ext))
                return BadRequest(new { message = "Le CV doit être PDF, DOC ou DOCX." });
            if (req.CvFile.Length > 5_000_000)
                return BadRequest(new { message = "Le CV ne doit pas dépasser 5 MB." });
            var guid = $"{Guid.NewGuid()}{ext}";
            await using var s = System.IO.File.Create(Path.Combine(uploadsDir, guid));
            await req.CvFile.CopyToAsync(s);
            cvFile = $"{req.CvFile.FileName}|{guid}";
        }

        if (req.MotivationLetterFile != null && req.MotivationLetterFile.Length > 0)
        {
            var ext = Path.GetExtension(req.MotivationLetterFile.FileName).ToLowerInvariant();
            if (!new[] { ".pdf", ".doc", ".docx" }.Contains(ext))
                return BadRequest(new { message = "La lettre de motivation doit être PDF, DOC ou DOCX." });
            if (req.MotivationLetterFile.Length > 5_000_000)
                return BadRequest(new { message = "La lettre de motivation ne doit pas dépasser 5 MB." });
            var guid = $"{Guid.NewGuid()}{ext}";
            await using var s = System.IO.File.Create(Path.Combine(uploadsDir, guid));
            await req.MotivationLetterFile.CopyToAsync(s);
            motivationFile = $"{req.MotivationLetterFile.FileName}|{guid}";
        }

        var application = new JobApplication
        {
            JobOfferId = req.JobOfferId,
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            Email = req.Email.Trim(),
            Phone = req.Phone?.Trim(),
            CvFile = cvFile,
            MotivationLetterFile = motivationFile,
            Message = req.Message?.Trim(),
            Status = ApplicationStatus.Nouvelle,
            AppliedAt = DateTime.UtcNow
        };

        _db.JobApplications.Add(application);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Votre candidature a bien été envoyée. Nous vous contacterons prochainement.",
            applicationId = application.Id
        });
    }

    [HttpGet, Authorize(Policy = "HrWrite")]
    public async Task<IActionResult> GetApplications(
        [FromQuery] int? jobOfferId,
        [FromQuery] string? status)
    {
        var q = _db.JobApplications.Include(a => a.JobOffer).AsQueryable();
        if (jobOfferId.HasValue)
            q = q.Where(a => a.JobOfferId == jobOfferId.Value);
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ApplicationStatus>(status, true, out var st))
            q = q.Where(a => a.Status == st);

        var list = await q.OrderByDescending(a => a.AppliedAt).ToListAsync();
        return Ok(list.Select(a => new
        {
            a.Id,
            a.JobOfferId,
            jobTitle = a.JobOffer?.Title ?? "—",
            a.FirstName,
            a.LastName,
            fullName = a.FullName,
            a.Email,
            a.Phone,
            a.CvFile,
            a.MotivationLetterFile,
            a.Message,
            status = a.Status.ToString(),
            a.InternalNote,
            a.AppliedAt,
            a.UpdatedAt
        }));
    }

    [HttpPatch("{id:int}/status"), Authorize(Policy = "HrWrite")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateApplicationStatusRequest req)
    {
        if (!Enum.TryParse<ApplicationStatus>(req.Status, true, out var st))
            return BadRequest(new { message = $"Statut invalide : {req.Status}" });

        var app = await _db.JobApplications.FindAsync(id);
        if (app is null) return NotFound(new { message = "Candidature introuvable." });

        app.Status = st;
        app.InternalNote = req.InternalNote?.Trim() ?? app.InternalNote;
        app.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { app.Id, status = app.Status.ToString(), app.InternalNote });
    }

    [HttpDelete("{id:int}"), Authorize(Policy = "HrWrite")]
    public async Task<IActionResult> DeleteApplication(int id)
    {
        var app = await _db.JobApplications.FindAsync(id);
        if (app is null) return NotFound(new { message = "Candidature introuvable." });
        _db.JobApplications.Remove(app);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Téléchargement sécurisé des fichiers candidature ─────────────────────
    // - Protégé par [Authorize] → le token JWT est vérifié côté serveur
    // - Le frontend envoie le token via fetch() + authHeaders() (plus de <a href> direct)
    // - Content-Disposition "inline" pour PDF (affichage dans le navigateur via Blob URL)
    // - Content-Disposition "attachment" pour DOC/DOCX (téléchargement forcé)
    // - Exposition du header Content-Disposition via CORS pour que le Blob URL fonctionne
    [HttpGet("file/{uniqueName}"), Authorize(Policy = "HrWrite")]
    public IActionResult DownloadFile(string uniqueName)
    {
        if (uniqueName.Contains("..") || uniqueName.Contains('/') || uniqueName.Contains('\\'))
            return BadRequest(new { message = "Nom de fichier invalide." });

        var path = Path.Combine(_env.ContentRootPath, "Uploads", "applications", uniqueName);
        if (!System.IO.File.Exists(path))
            return NotFound(new { message = "Fichier introuvable." });

        var ext = Path.GetExtension(uniqueName).ToLowerInvariant();
        var mime = ext switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };

        var encodedName = System.Net.WebUtility.UrlEncode(uniqueName);
        var disposition = ext == ".pdf"
            ? $"inline; filename=\"{encodedName}\"; filename*=UTF-8''{encodedName}"
            : $"attachment; filename=\"{encodedName}\"; filename*=UTF-8''{encodedName}";

        Response.Headers["Content-Disposition"] = disposition;
        Response.Headers["Access-Control-Expose-Headers"] = "Content-Disposition";

        return PhysicalFile(path, mime);
    }
}

// ── Request records ────────────────────────────────────────────────────────────

public record JobOfferRequest(
    string Title, string? Description, string? Missions,
    string? Benefits, string? Process, string? Requirements,
    string ContractType, string AcademicLevel, string PostType,
    string? Location, string? Duration, string? Salary,
    string? Skills, DateTime? Deadline, bool? IsActive);

public class JobApplicationFormRequest
{
    public int JobOfferId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public IFormFile? CvFile { get; set; }
    public IFormFile? MotivationLetterFile { get; set; }
    public string? Message { get; set; }
}

public record UpdateApplicationStatusRequest(string Status, string? InternalNote);