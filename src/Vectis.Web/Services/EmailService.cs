using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Vectis.Web.Services;

public sealed class SmtpOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "Vectis";
    public bool EnableSsl { get; set; } = true;
}

public sealed record EmailSendResult(bool Sent, string Message)
{
    public static EmailSendResult Skipped(string reason) => new(false, reason);
    public static EmailSendResult Success() => new(true, "E-mail envoye.");
}

public sealed class EmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpOptions> options, ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(string toEmail, string subject, string body)
    {
        if (!_options.Enabled)
        {
            return EmailSendResult.Skipped("Envoi e-mail desactive. Le lien manuel reste disponible.");
        }

        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            return EmailSendResult.Skipped("Configuration SMTP incomplete. Le lien manuel reste disponible.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
        }

        try
        {
            await client.SendMailAsync(message);
            return EmailSendResult.Success();
        }
        catch (SmtpException ex)
        {
            _logger.LogWarning(ex, "Invitation email delivery failed for {Email}", toEmail);
            var detail = string.IsNullOrWhiteSpace(ex.Message) ? "Erreur SMTP inconnue." : ex.Message;
            return new EmailSendResult(false, $"L'e-mail n'a pas pu etre envoye ({detail}). Le lien manuel reste disponible.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invitation email delivery failed for {Email}", toEmail);
            return new EmailSendResult(false, "L'e-mail n'a pas pu etre envoye. Le lien manuel reste disponible.");
        }
    }
}
