using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace tunav_backend.Controllers;

[ApiController]
[Route("api/uploads")]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UploadController> _logger;

    private string CollaborationUploadPath => Path.Combine(_env.ContentRootPath, "Uploads", "collaborations");
    private string SolutionUploadPath => Path.Combine(
        _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
        "uploads",
        "solutions");

    public UploadController(IWebHostEnvironment env, ILogger<UploadController> logger)
    {
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Upload un ou plusieurs fichiers de collaboration et retourne les noms sauvegardes.
    /// </summary>
    [HttpPost("collaboration")]
    [RequestSizeLimit(50_000_000)] // 50 MB max total
    public async Task<IActionResult> Upload(List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { message = "Aucun fichier fourni." });

        Directory.CreateDirectory(CollaborationUploadPath);

        var savedNames = new List<string>();
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };

        foreach (var file in files)
        {
            if (file.Length == 0) continue;
            if (file.Length > 10_000_000)
            {
                return BadRequest(new { message = $"Le fichier '{file.FileName}' depasse 10 MB." });
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return BadRequest(new { message = $"Extension non autorisee : {ext}" });

            var uniqueName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(CollaborationUploadPath, uniqueName);

            await using var stream = System.IO.File.Create(filePath);
            await file.CopyToAsync(stream);

            savedNames.Add($"{file.FileName}|{uniqueName}");
            _logger.LogInformation("Fichier uploade : {OriginalName} -> {UniqueName}", file.FileName, uniqueName);
        }

        return Ok(new
        {
            message = $"{savedNames.Count} fichier(s) uploade(s).",
            files = savedNames
        });
    }

    /// <summary>
    /// Telecharger un fichier de collaboration par son nom unique.
    /// </summary>
    [HttpGet("{uniqueName}")]
    public IActionResult Download(string uniqueName)
    {
        if (uniqueName.Contains("..") || uniqueName.Contains('/') || uniqueName.Contains('\\'))
            return BadRequest(new { message = "Nom de fichier invalide." });

        var filePath = Path.Combine(CollaborationUploadPath, uniqueName);
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { message = "Fichier introuvable." });

        var ext = Path.GetExtension(uniqueName).ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

        var fileBytes = System.IO.File.ReadAllBytes(filePath);

        // Pour PDF, images et DOC : ouvrir inline dans le navigateur
        // Pour les autres : forcer le téléchargement
        bool openInline = ext is ".pdf" or ".jpg" or ".jpeg" or ".png";

        if (openInline)
        {
            // Retourner en streaming avec header Content-Disposition: inline
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{System.Net.WebUtility.UrlEncode(uniqueName)}\"";
            return File(fileBytes, contentType);
        }

        return File(fileBytes, contentType, uniqueName);
    }

    /// <summary>
    /// Upload d'image de solution (cards + detail) et retourne une URL exploitable par le frontend.
    /// </summary>
    [HttpPost("solution-image")]
    [Authorize(Policy = "SolutionWrite")]
    [RequestSizeLimit(10_000_000)] // 10 MB
    public async Task<IActionResult> UploadSolutionImage(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Aucune image fournie." });

        if (file.Length > 5_000_000)
            return BadRequest(new { message = "L'image depasse 5 MB." });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { message = $"Extension non autorisee : {ext}" });

        Directory.CreateDirectory(SolutionUploadPath);

        var uniqueName = $"solution_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(SolutionUploadPath, uniqueName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/solutions/{uniqueName}";
        var absoluteUrl = $"{Request.Scheme}://{Request.Host}{relativeUrl}";

        _logger.LogInformation("Image solution uploadee : {OriginalName} -> {StoredName}", file.FileName, uniqueName);

        return Ok(new
        {
            message = "Image solution uploadee avec succes.",
            url = relativeUrl,
            absoluteUrl
        });
    }

    private static readonly string[] StandardImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    private string ContentUploadsRoot => Path.Combine(_env.ContentRootPath, "Uploads");

    private async Task<IActionResult> SaveImageToContentUploadsAsync(
        IFormFile? file,
        string subFolder,
        string filePrefix,
        long maxBytes)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Aucune image fournie." });

        if (file.Length > maxBytes)
            return BadRequest(new { message = $"L'image depasse {maxBytes / 1_000_000} MB." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!StandardImageExtensions.Contains(ext))
            return BadRequest(new { message = $"Extension non autorisee : {ext}" });

        var dir = Path.Combine(ContentUploadsRoot, subFolder);
        Directory.CreateDirectory(dir);

        var uniqueName = $"{filePrefix}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(dir, uniqueName);

        await using (var stream = System.IO.File.Create(filePath))
            await file.CopyToAsync(stream);

        var relativeUrl = $"/uploads/{subFolder.TrimEnd('/')}/{uniqueName}";
        _logger.LogInformation("Image {SubFolder} uploadee : {Original} -> {Stored}", subFolder, file.FileName, uniqueName);

        return Ok(new
        {
            message = "Image uploadee avec succes.",
            url = relativeUrl,
            absoluteUrl = $"{Request.Scheme}://{Request.Host}{relativeUrl}"
        });
    }

    [HttpPost("blog-cover-image")]
    [Authorize(Policy = "BlogWrite")]
    [RequestSizeLimit(10_000_000)]
    public Task<IActionResult> UploadBlogCoverImage(IFormFile? file) =>
        SaveImageToContentUploadsAsync(file, "blogs", "blog", 5_000_000);

    [HttpPost("event-cover-image")]
    [Authorize(Policy = "EventWrite")]
    [RequestSizeLimit(10_000_000)]
    public Task<IActionResult> UploadEventCoverImage(IFormFile? file) =>
        SaveImageToContentUploadsAsync(file, "events", "event", 5_000_000);

    [HttpPost("newsletter-cover-image")]
    [Authorize(Policy = "NewsletterWrite")]
    [RequestSizeLimit(10_000_000)]
    public Task<IActionResult> UploadNewsletterCoverImage(IFormFile? file) =>
        SaveImageToContentUploadsAsync(file, "newsletters", "newsletter", 5_000_000);

    [HttpPost("team-photo")]
    [Authorize(Policy = "TeamWrite")]
    [RequestSizeLimit(10_000_000)]
    public Task<IActionResult> UploadTeamPhoto(IFormFile? file) =>
        SaveImageToContentUploadsAsync(file, "team", "team", 5_000_000);

    [HttpPost("training-partner-image")]
    [Authorize(Policy = "EventWrite")]
    [RequestSizeLimit(10_000_000)]
    public Task<IActionResult> UploadTrainingPartnerImage(IFormFile? file) =>
        SaveImageToContentUploadsAsync(file, "training-partners", "partner", 5_000_000);

    [HttpPost("sector-image")]
    [Authorize(Policy = "SectorWrite")]
    [RequestSizeLimit(10_000_000)]
    public Task<IActionResult> UploadSectorImage(IFormFile? file) =>
        SaveImageToContentUploadsAsync(file, "sectors", "sector", 5_000_000);

    /// <summary>Fichier vidéo du pack (MP4/WebM), servi sous /uploads/packs/… — les liens YouTube restent possibles via le champ URL.</summary>
    [HttpPost("pack-video")]
    [Authorize(Policy = "SolutionWrite")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadPackVideo(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Aucune video fournie." });

        if (file.Length > 40_000_000)
            return BadRequest(new { message = "La video depasse 40 MB." });

        var allowed = new[] { ".mp4", ".webm" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest(new { message = $"Extension non autorisee : {ext} (MP4 ou WebM)." });

        var dir = Path.Combine(ContentUploadsRoot, "packs");
        Directory.CreateDirectory(dir);

        var uniqueName = $"pack_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(dir, uniqueName);

        await using (var stream = System.IO.File.Create(filePath))
            await file.CopyToAsync(stream);

        var relativeUrl = $"/uploads/packs/{uniqueName}";
        _logger.LogInformation("Video pack uploadee : {Original} -> {Stored}", file.FileName, uniqueName);

        return Ok(new
        {
            message = "Video uploadee avec succes.",
            url = relativeUrl,
            absoluteUrl = $"{Request.Scheme}://{Request.Host}{relativeUrl}"
        });
    }
}
