using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Services;

public interface INewsletterEmailService
{
    Task SendSubscriptionConfirmationEmailAsync(NewsletterSubscriber subscriber, CancellationToken ct = default);
    Task<NewsletterNotifyResult> NotifyEditionAsync(int newsletterId, CancellationToken ct = default);
}

public record NewsletterNotifyResult(int SubscribersTargeted, int Sent, int Failed);

public sealed class NewsletterEmailService : INewsletterEmailService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IEmailSender _email;
    private readonly ILogger<NewsletterEmailService> _logger;

    public NewsletterEmailService(
        AppDbContext db,
        IConfiguration config,
        IEmailSender email,
        ILogger<NewsletterEmailService> logger)
    {
        _db = db;
        _config = config;
        _email = email;
        _logger = logger;
    }

    public async Task SendSubscriptionConfirmationEmailAsync(NewsletterSubscriber sub, CancellationToken ct = default)
    {
        try
        {
            var baseUrl = GetPublicBaseUrl();
            var unsubUrl = $"{baseUrl}/api/newsletter-subscribers/unsubscribe?token={sub.UnsubscribeToken}";

            var html = $@"<!DOCTYPE html><html lang='fr'><head><meta charset='utf-8'/></head>
<body style='font-family:Arial,Helvetica,sans-serif;color:#1e2d5a;background:#f0f4ff;margin:0;padding:24px'>
  <div style='max-width:560px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(26,42,108,.1)'>
    <div style='background:linear-gradient(135deg,#1a2a6c,#00c8d4);padding:28px 32px;text-align:center'>
      <h2 style='color:#fff;margin:0;font-size:22px'>📧 Bienvenue dans la Newsletter TUNAV !</h2>
    </div>
    <div style='padding:28px 32px'>
      <p style='font-size:15px;line-height:1.7'>Bonjour,</p>
      <p style='font-size:15px;line-height:1.7'>
        Votre abonnement à la newsletter <strong>TUNAV Insights</strong> est confirmé.
        Vous recevrez désormais chaque mois nos actualités, innovations et tendances en GPS tracking et IoT.
      </p>
      <div style='text-align:center;margin:24px 0'>
        <div style='display:inline-block;background:#f0f4ff;border-radius:8px;padding:16px 24px'>
          <p style='margin:0;font-size:13px;color:#6b7db3'>✅ Pas de spam &nbsp;|&nbsp; 📅 1 newsletter/mois &nbsp;|&nbsp; 🔒 Données protégées</p>
        </div>
      </div>
    </div>
    <div style='padding:14px 32px;background:#f8faff;border-top:1px solid #dce4f5;text-align:center'>
      <p style='margin:0;color:#b0bdd8;font-size:11px'>
        TUNAV IT Group · <a href='{unsubUrl}' style='color:#00c8d4'>Se désabonner</a>
      </p>
    </div>
  </div>
</body></html>";

            await _email.SendHtmlAsync(
                toEmail: sub.Email,
                toName: sub.Email,
                subject: "✅ Confirmation d'abonnement — TUNAV Newsletter",
                htmlBody: html,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur confirmation abonnement {Email}", sub.Email);
        }
    }

    public async Task<NewsletterNotifyResult> NotifyEditionAsync(int newsletterId, CancellationToken ct = default)
    {
        var newsletter = await _db.Newsletters.FirstOrDefaultAsync(n => n.Id == newsletterId, ct);
        if (newsletter is null)
            throw new KeyNotFoundException("Newsletter introuvable.");

        var subscribers = await _db.NewsletterSubscribers
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.SubscribedAt)
            .ToListAsync(ct);

        if (subscribers.Count == 0)
        {
            _logger.LogInformation("Newsletter notify: aucun abonné actif.");
            return new NewsletterNotifyResult(0, 0, 0);
        }

        var sent = 0;
        var failed = 0;

        _logger.LogInformation("Newsletter notify: envoi editionId={Id} à {Count} abonnés...", newsletter.Id, subscribers.Count);

        foreach (var sub in subscribers)
        {
            try
            {
                var html = BuildNewsletterEmailBody(newsletter, sub);
                await _email.SendHtmlAsync(
                    toEmail: sub.Email,
                    toName: sub.Email,
                    subject: $"📧 Nouvelle Newsletter : {newsletter.Title}",
                    htmlBody: html,
                    ct: ct);
                sent++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "Erreur envoi newsletter editionId={EditionId} à {Email}", newsletter.Id, sub.Email);
            }
        }

        _logger.LogInformation("Newsletter notify: terminé editionId={Id}. sent={Sent} failed={Failed}", newsletter.Id, sent, failed);
        return new NewsletterNotifyResult(subscribers.Count, sent, failed);
    }

    private string BuildNewsletterEmailBody(Newsletter newsletter, NewsletterSubscriber sub)
    {
        static string E(string? s) => System.Web.HttpUtility.HtmlEncode(s ?? "");

        var baseUrl = GetPublicBaseUrl();
        var unsubUrl = $"{baseUrl}/api/newsletter-subscribers/unsubscribe?token={sub.UnsubscribeToken}";

        var tocHtml = newsletter.TocLines.Length > 0
            ? "<ul style='padding-left:18px;margin:0'>" +
              string.Join("", newsletter.TocLines.Select(l =>
                  $"<li style='font-size:13px;line-height:1.8;color:#1e2d5a'>✅ {E(l)}</li>")) +
              "</ul>"
            : "";

        var pdfUrl = NormalizeUrl(baseUrl, newsletter.PdfUrl);
        var pdfBtn = !string.IsNullOrWhiteSpace(pdfUrl)
            ? $@"<div style='text-align:center;margin:24px 0'>
                  <a href='{E(pdfUrl)}'
                     style='display:inline-block;background:linear-gradient(135deg,#1a2a6c,#00c8d4);
                            color:#fff;padding:13px 28px;border-radius:8px;
                            text-decoration:none;font-weight:700;font-size:15px'>
                    📥 Télécharger le PDF
                  </a>
                </div>"
            : "";

        return $@"<!DOCTYPE html><html lang='fr'><head><meta charset='utf-8'/></head>
<body style='font-family:Arial,Helvetica,sans-serif;color:#1e2d5a;background:#f0f4ff;margin:0;padding:24px'>
  <div style='max-width:600px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(26,42,108,.1)'>

    <div style='background:linear-gradient(135deg,#1a2a6c,#00c8d4);padding:28px 32px'>
      <p style='color:rgba(255,255,255,.75);margin:0 0 6px;font-size:13px'>📧 Newsletter TUNAV Insights</p>
      <h2 style='color:#fff;margin:0;font-size:22px'>{E(newsletter.Title)}</h2>
    </div>

    <div style='padding:28px 32px'>
      {(newsletter.Summary != null ? $"<p style='font-size:14px;line-height:1.75;color:#6b7db3;border-left:3px solid #00c8d4;padding-left:14px;margin-bottom:20px'>{E(newsletter.Summary)}</p>" : "")}

      {(tocHtml.Length > 0 ? $@"<div style='background:#f8faff;border:1.5px solid #dce4f5;border-radius:8px;padding:16px 20px;margin-bottom:20px'>
        <strong style='font-size:13px;color:#1e2d5a;display:block;margin-bottom:10px'>📋 Au sommaire :</strong>
        {tocHtml}
      </div>" : "")}

      {pdfBtn}
    </div>

    <div style='padding:16px 32px;background:#f8faff;border-top:1px solid #dce4f5;text-align:center'>
      <p style='margin:0;color:#b0bdd8;font-size:11px'>
        TUNAV IT Group — Newsletter ·
        <a href='{unsubUrl}' style='color:#00c8d4'>Se désabonner</a>
      </p>
    </div>
  </div>
</body></html>";
    }

    private string GetPublicBaseUrl()
    {
        var baseUrl = _config["PublicBaseUrl"] ?? "";
        baseUrl = baseUrl.Trim();
        if (baseUrl.EndsWith("/")) baseUrl = baseUrl.TrimEnd('/');

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning("PublicBaseUrl non configuré — fallback sur http://localhost:5057");
            return "http://localhost:5057";
        }

        return baseUrl;
    }

    private static string? NormalizeUrl(string baseUrl, string? urlOrPath)
    {
        if (string.IsNullOrWhiteSpace(urlOrPath)) return null;
        var s = urlOrPath.Trim();
        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return s;

        if (!s.StartsWith("/")) s = "/" + s;
        return baseUrl + s;
    }
}
