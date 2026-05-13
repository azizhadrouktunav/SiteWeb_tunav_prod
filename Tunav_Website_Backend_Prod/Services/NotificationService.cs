using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using tunav_backend.Models;

namespace tunav_backend.Services
{
    // ══════════════════════════════════════════════════════════════════════════
    //  Interface
    // ══════════════════════════════════════════════════════════════════════════

    public interface INotificationService
    {
        /// <summary>
        /// US-EV12 — Envoie une notification email au département concerné
        /// dès qu'une nouvelle demande de collaboration est soumise.
        /// </summary>
        Task NotifyNewCollaborationAsync(CollaborationRequest request, string eventTitle);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Implémentation via l'API HTTP Brevo (port 443 — HTTPS)
    //
    //  Pourquoi HTTP et non SMTP ?
    //  Le port 587 était bloqué par l'environnement local (antivirus / proxy).
    //  L'API REST Brevo passe par HTTPS port 443, jamais filtré.
    //
    //  Configuration requise dans appsettings.json :
    //    "Resend": { "ApiKey": "re_...", "FromEmail": "...", "FromName": "TUNAV" }
    //    "Smtp":   { "NotifyRH": "...", "NotifyMarketing": "...", "NotifyTo": "..." }
    // ══════════════════════════════════════════════════════════════════════════

    public class SmtpNotificationService : INotificationService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpNotificationService> _logger;
        private readonly IHttpClientFactory _httpFactory;

        private const string ResendApiUrl = "https://api.resend.com/emails";

        public SmtpNotificationService(
            IConfiguration config,
            ILogger<SmtpNotificationService> logger,
            IHttpClientFactory httpFactory)
        {
            _config = config;
            _logger = logger;
            _httpFactory = httpFactory;
        }

        // ── Notifications internes (collaborations) ───────────────────────────

        public async Task NotifyNewCollaborationAsync(CollaborationRequest request, string eventTitle)
        {
            var apiKey = _config["Resend:ApiKey"] ?? "";
            var from = _config["Resend:FromEmail"] ?? "onboarding@resend.dev";
            var fromName = _config["Resend:FromName"] ?? "TUNAV Backoffice";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("US-EV12 : Resend:ApiKey non configuré — notification ignorée.");
                return;
            }

            var recipients = ResolveRecipients(request.CollaborationType);
            if (recipients.Count == 0)
            {
                _logger.LogWarning(
                    "US-EV12 : Aucun destinataire pour le type {Type} — notification ignorée.",
                    request.CollaborationType);
                return;
            }

            var (subject, htmlBody) = BuildEmail(request, eventTitle);

            try
            {
                _logger.LogInformation(
                    "US-EV12 : Envoi Resend [{Type}] → {Recipients}",
                    request.CollaborationType, string.Join(", ", recipients));

                await PostResendAsync(apiKey, from, fromName,
                    recipients.ToArray(), subject, htmlBody);

                _logger.LogInformation("US-EV12 : Email envoyé avec succès.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "US-EV12 : Échec envoi [{Type}] : {Message}",
                    request.CollaborationType, ex.Message);
            }
        }

        // ── Appel HTTP Resend ─────────────────────────────────────────────────

        private async Task PostResendAsync(
            string apiKey, string fromEmail, string fromName,
            string[] toEmails, string subject, string htmlBody)
        {
            var payload = new
            {
                from = $"{fromName} <{fromEmail}>",
                to = toEmails,
                subject = subject,
                html = htmlBody
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            client.DefaultRequestHeaders.Accept
                  .Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.PostAsync(ResendApiUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Resend API erreur {(int)response.StatusCode} : {err}");
            }
        }
        // ══════════════════════════════════════════════════════════════════════

        private List<string> ResolveRecipients(CollaborationType type)
        {
            var result = new List<string>();

            void Add(string key)
            {
                var val = _config[key] ?? "";
                result.AddRange(
                    val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Where(e => e.Contains('@'))
                );
            }

            switch (type)
            {
                case CollaborationType.PropositionEvenement:
                    Add("Smtp:NotifyMarketing");
                    Add("Smtp:NotifyRH");
                    break;

                case CollaborationType.Collaboration:
                case CollaborationType.DemandeFormation:
                    Add("Smtp:NotifyRH");
                    if (result.Count == 0)
                        Add("Smtp:NotifyTo");
                    break;

                default:
                    Add("Smtp:NotifyTo");
                    break;
            }

            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  Construction email (sujet + HTML) selon le type
        // ══════════════════════════════════════════════════════════════════════

        private static (string subject, string html) BuildEmail(
            CollaborationRequest req, string eventTitle)
        {
            return req.CollaborationType switch
            {
                CollaborationType.PropositionEvenement => BuildPropositionEmail(req, eventTitle),
                CollaborationType.Collaboration => BuildCollaborationEmail(req),
                CollaborationType.DemandeFormation => BuildDemandeFormationEmail(req),
                _ => BuildGenericEmail(req, eventTitle)
            };
        }

        // ── 1. Proposition d'événement ────────────────────────────────────────

        private static (string, string) BuildPropositionEmail(
            CollaborationRequest req, string eventTitle)
        {
            static string E(string? s) => System.Web.HttpUtility.HtmlEncode(s ?? "—");

            var subject = $"[TUNAV] 📅 Proposition d'événement — {E(req.Organization)}";

            var rows = Rows(
                ("Événement", E(eventTitle)),
                ("Organisme", E(req.Organization)),
                ("Responsable", E(req.FullName)),
                ("Email", $"<a href='mailto:{E(req.Email)}' style='color:#00c8d4'>{E(req.Email)}</a>"),
                ("Téléphone", E(req.Phone)),
                ("Adresse", E(req.Address)),
                ("Message", E(req.Message)),
                ("Pièces jointes", E(req.AttachmentNames)),
                ("Soumis le", req.SubmittedAt.ToString("dd/MM/yyyy HH:mm") + " UTC")
            );

            return (subject, Layout(
                gradient: "135deg,#0a1628,#e63946",
                emoji: "📅",
                title: "Nouvelle proposition d'événement",
                sub: $"De : <strong style='color:#fff'>{E(req.Organization)}</strong>",
                badge: ("📅 Proposition événement", "#e63946"),
                rows: rows,
                tabLink: "collab"
            ));
        }

        // ── 2. Demande de collaboration — pôle formation ──────────────────────

        private static (string, string) BuildCollaborationEmail(CollaborationRequest req)
        {
            static string E(string? s) => System.Web.HttpUtility.HtmlEncode(s ?? "—");

            var subject = $"[TUNAV] 🎓 Nouveau partenaire formation — {E(req.Organization)}";

            var rows = Rows(
                ("Établissement", E(req.Organization)),
                ("Responsable", E(req.FullName)),
                ("Email", $"<a href='mailto:{E(req.Email)}' style='color:#00c8d4'>{E(req.Email)}</a>"),
                ("Téléphone", E(req.Phone)),
                ("Adresse", E(req.Address)),
                ("Description", E(req.Message)),
                ("Pièces jointes", E(req.AttachmentNames)),
                ("Soumis le", req.SubmittedAt.ToString("dd/MM/yyyy HH:mm") + " UTC")
            );

            return (subject, Layout(
                gradient: "135deg,#1a2a6c,#7b2ff7",
                emoji: "🎓",
                title: "Nouveau partenaire formation",
                sub: $"Établissement : <strong style='color:#fff'>{E(req.Organization)}</strong>",
                badge: ("🎓 Demande de collaboration", "#7b2ff7"),
                rows: rows,
                tabLink: "collab"
            ));
        }

        // ── 3. Demande de formation (avec Homologation MALEK) ─────────────────

        private static (string, string) BuildDemandeFormationEmail(CollaborationRequest req)
        {
            static string E(string? s) => System.Web.HttpUtility.HtmlEncode(s ?? "—");

            var homologue = req.IsHomologueMalek.HasValue
                ? (req.IsHomologueMalek.Value ? "✅ Oui" : "❌ Non")
                : "—";

            var subject = $"[TUNAV] 📋 Demande de formation — {E(req.Organization)}";

            var rows = Rows(
                ("Établissement", E(req.Organization)),
                ("Responsable", E(req.FullName)),
                ("Email", $"<a href='mailto:{E(req.Email)}' style='color:#00c8d4'>{E(req.Email)}</a>"),
                ("Téléphone", E(req.Phone)),
                ("Adresse", E(req.Address)),
                ("Description", E(req.Message)),
                ("Homologation MALEK", homologue),
                ("Pièces jointes", E(req.AttachmentNames)),
                ("Soumis le", req.SubmittedAt.ToString("dd/MM/yyyy HH:mm") + " UTC")
            );

            return (subject, Layout(
                gradient: "135deg,#00695c,#00c8d4",
                emoji: "📋",
                title: "Nouvelle demande de formation",
                sub: $"Établissement : <strong style='color:#fff'>{E(req.Organization)}</strong>",
                badge: ("📋 Demande de formation", "#00695c"),
                rows: rows,
                tabLink: "formation"
            ));
        }

        // ── 4. Email générique (fallback) ─────────────────────────────────────

        private static (string, string) BuildGenericEmail(
            CollaborationRequest req, string eventTitle)
        {
            static string E(string? s) => System.Web.HttpUtility.HtmlEncode(s ?? "—");

            var subject = $"[TUNAV] 🤝 Nouvelle demande — {E(req.Organization)}";

            var rows = Rows(
                ("Événement", E(eventTitle)),
                ("Organisme", E(req.Organization)),
                ("Responsable", E(req.FullName)),
                ("Email", $"<a href='mailto:{E(req.Email)}' style='color:#00c8d4'>{E(req.Email)}</a>"),
                ("Téléphone", E(req.Phone)),
                ("Message", E(req.Message)),
                ("Soumis le", req.SubmittedAt.ToString("dd/MM/yyyy HH:mm") + " UTC")
            );

            return (subject, Layout(
                gradient: "135deg,#1a2a6c,#00c8d4",
                emoji: "🤝",
                title: "Nouvelle demande de collaboration",
                sub: $"De : <strong style='color:#fff'>{E(req.Organization)}</strong>",
                badge: ("🤝 Collaboration", "#1565c0"),
                rows: rows,
                tabLink: "collab"
            ));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  Template HTML commun
        // ══════════════════════════════════════════════════════════════════════

        private static string Layout(
            string gradient, string emoji,
            string title, string sub,
            (string label, string color) badge,
            string rows, string tabLink)
        {
            var url = $"http://localhost:5057/backoffice/collaborations";

            return $@"<!DOCTYPE html>
<html lang=""fr"">
<head><meta charset=""utf-8""/><meta name=""viewport"" content=""width=device-width,initial-scale=1""/></head>
<body style=""font-family:Arial,Helvetica,sans-serif;color:#1e2d5a;background:#f0f4ff;margin:0;padding:24px;"">
  <div style=""max-width:620px;margin:0 auto;background:#fff;border-radius:16px;overflow:hidden;
              box-shadow:0 6px 32px rgba(26,42,108,0.12);"">

    <!-- HEADER -->
    <div style=""background:linear-gradient({gradient});padding:32px;"">
      <div style=""font-size:42px;margin-bottom:10px;line-height:1"">{emoji}</div>
      <h2 style=""color:#fff;margin:0 0 6px;font-size:21px;font-weight:800;"">{title}</h2>
      <p style=""color:rgba(255,255,255,0.82);margin:0;font-size:14px;"">{sub}</p>
    </div>

    <!-- BADGE TYPE -->
    <div style=""padding:16px 32px 0;"">
      <span style=""display:inline-block;background:{badge.color};color:#fff;
                    font-size:12px;font-weight:700;padding:4px 14px;
                    border-radius:20px;letter-spacing:.3px;"">{badge.label}</span>
    </div>

    <!-- DONNÉES -->
    <div style=""padding:20px 32px 8px;"">
      <table style=""width:100%;border-collapse:collapse;"">{rows}</table>
    </div>

    <!-- CTA BACKOFFICE -->
    <div style=""margin:8px 32px 28px;padding:18px 20px;background:#f0f4ff;
                border-radius:10px;border-left:4px solid #00c8d4;"">
      <p style=""margin:0 0 10px;font-size:13px;color:#6b7db3;font-weight:600;"">
        ⚡ Action requise — traitez cette demande dans le backoffice :
      </p>
      <a href=""{url}""
         style=""display:inline-block;background:linear-gradient(135deg,#1a2a6c,#00c8d4);
                color:#fff;text-decoration:none;padding:11px 24px;border-radius:8px;
                font-size:14px;font-weight:700;"">→ Ouvrir le backoffice</a>
      <p style=""margin:10px 0 0;font-size:11px;color:#b0bdd8;"">
        Lien direct : {url}
      </p>
    </div>

    <!-- FOOTER -->
    <div style=""padding:16px 32px;background:#f8faff;border-top:1px solid #dce4f5;"">
      <p style=""margin:0;color:#b0bdd8;font-size:11px;line-height:1.6;"">
        <strong style=""color:#6b7db3;"">TUNAV IT Group</strong> — Notification automatique US-EV12<br/>
        Cet email est généré automatiquement, merci de ne pas y répondre.
      </p>
    </div>

  </div>
</body>
</html>";
        }

        // ── Générateur de lignes de tableau ──────────────────────────────────

        private static string Rows(params (string label, string value)[] entries)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var (label, value) in entries)
            {
                if (string.IsNullOrWhiteSpace(value) || value == "—") continue;
                var l = System.Web.HttpUtility.HtmlEncode(label);
                sb.Append(
                    "<tr>" +
                    $"<td style=\"padding:10px 14px;background:#f8faff;font-weight:600;font-size:13px;" +
                    $"width:170px;border:1px solid #dce4f5;color:#6b7db3;vertical-align:top;\">{l}</td>" +
                    $"<td style=\"padding:10px 14px;border:1px solid #dce4f5;font-size:13px;" +
                    $"color:#1e2d5a;\">{value}</td>" +
                    "</tr>"
                );
            }
            return sb.ToString();
        }
    }
}