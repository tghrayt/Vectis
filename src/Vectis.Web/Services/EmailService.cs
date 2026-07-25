using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

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

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        try
        {
            using var client = new SmtpClient();
            var secureSocketOptions = _options.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions);

            if (!string.IsNullOrWhiteSpace(_options.UserName))
            {
                await client.AuthenticateAsync(_options.UserName, _options.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            return EmailSendResult.Success();
        }
        catch (MailKit.CommandException ex)
        {
            _logger.LogWarning(ex, "Invitation email delivery failed for {Email}", toEmail);
            var detail = string.IsNullOrWhiteSpace(ex.Message) ? "Erreur SMTP inconnue." : ex.Message;
            return new EmailSendResult(false, $"L'e-mail n'a pas pu etre envoye ({detail}). Le lien manuel reste disponible.");
        }
        catch (MailKit.ProtocolException ex)
        {
            _logger.LogWarning(ex, "Invitation email delivery failed for {Email}", toEmail);
            var detail = string.IsNullOrWhiteSpace(ex.Message) ? "Erreur SMTP inconnue." : ex.Message;
            return new EmailSendResult(false, $"L'e-mail n'a pas pu etre envoye ({detail}). Le lien manuel reste disponible.");
        }
        catch (MailKit.Security.AuthenticationException ex)
        {
            _logger.LogWarning(ex, "Invitation email authentication failed for {Email}", toEmail);
            return new EmailSendResult(false, "L'authentification SMTP a echoue. Verifie le login SMTP et la cle SMTP Brevo.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invitation email delivery failed for {Email}", toEmail);
            return new EmailSendResult(false, "L'e-mail n'a pas pu etre envoye. Le lien manuel reste disponible.");
        }
    }
}
