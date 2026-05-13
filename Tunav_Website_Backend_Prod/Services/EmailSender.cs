using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace tunav_backend.Services;

public interface IEmailSender
{
    Task SendHtmlAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default);
}

public sealed class MailKitEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(IConfiguration config, ILogger<MailKitEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendHtmlAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        var host = _config["Smtp:Host"] ?? "smtp.gmail.com";
        var portStr = _config["Smtp:Port"] ?? "587";
        var user = _config["Smtp:User"] ?? "";
        var password = _config["Smtp:Password"] ?? "";
        var from = _config["Smtp:From"] ?? user;
        var fromName = _config["Smtp:FromName"] ?? "TUNAV";

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            // Ne pas ignorer silencieusement: ça donne l'impression que l'envoi fonctionne
            // alors qu'aucun email ne peut partir.
            throw new InvalidOperationException("SMTP non configuré: renseignez Smtp:User et Smtp:Password.");
        }

        if (!int.TryParse(portStr, out var port)) port = 587;

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(fromName, from));
        msg.To.Add(new MailboxAddress(string.IsNullOrWhiteSpace(toName) ? toEmail : toName, toEmail));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = 20_000;

        try
        {
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(user, password, ct);
            await client.SendAsync(msg, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed (host={Host} port={Port} from={From} to={To} subject={Subject})",
                host, port, from, toEmail, subject);
            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(quit: true, ct);
            }
        }
    }
}
