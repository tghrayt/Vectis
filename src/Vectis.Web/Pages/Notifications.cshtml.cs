using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

[Authorize]
public sealed class NotificationsModel : PageModel
{
    private readonly CurrentUser _currentUser;
    private readonly NotificationService _notificationService;
    private readonly EmailService _emailService;
    private readonly SmtpOptions _smtpOptions;

    public NotificationsModel(
        CurrentUser currentUser,
        NotificationService notificationService,
        EmailService emailService,
        IOptions<SmtpOptions> smtpOptions)
    {
        _currentUser = currentUser;
        _notificationService = notificationService;
        _emailService = emailService;
        _smtpOptions = smtpOptions.Value;
    }

    public NotificationOverview? Overview { get; private set; }
    public NotificationSendSummary? SendSummary { get; private set; }
    public EmailSendResult? TestResult { get; private set; }
    public bool CanSend { get; private set; }
    public bool SmtpConfigured => _smtpOptions.Enabled
        && !string.IsNullOrWhiteSpace(_smtpOptions.Host)
        && !string.IsNullOrWhiteSpace(_smtpOptions.FromEmail);
    public string SmtpStatusLabel => SmtpConfigured ? "SMTP pret" : _smtpOptions.Enabled ? "SMTP incomplet" : "E-mail desactive";
    public string SmtpStatusClass => SmtpConfigured ? "ok" : _smtpOptions.Enabled ? "warning" : "danger";
    public string AutomaticStatusLabel => Overview?.Preferences.AutomaticEmailEnabled == true ? "Automatique active" : "Automatique desactive";
    public string LastDeliveryLabel => Overview?.RecentDeliveries.FirstOrDefault()?.CreatedAt.LocalDateTime.ToString("g") ?? "Aucun envoi";
    public int SentCount => Overview?.RecentDeliveries.Count(item => item.Status == "sent") ?? 0;
    public int FailedCount => Overview?.RecentDeliveries.Count(item => item.Status == "failed") ?? 0;
    public int IgnoredCount => Overview?.RecentDeliveries.Count(item => item.Status == "ignored") ?? 0;

    public async Task<IActionResult> OnGetAsync(int? sent, int? skipped, int? failed, string? test)
    {
        var context = await _currentUser.GetAsync();
        if (context?.Family is null)
        {
            return RedirectToPage("/Account/Login");
        }

        await LoadAsync(context);
        if (sent is not null || skipped is not null || failed is not null)
        {
            SendSummary = new NotificationSendSummary(sent ?? 0, skipped ?? 0, failed ?? 0);
        }
        if (test is not null)
        {
            TestResult = new EmailSendResult(test == "sent", test == "sent" ? "E-mail de test envoye." : "E-mail de test non envoye.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSendAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Family is null)
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            var summary = await _notificationService.SendNowAsync(context.User.Id, context.Family.Id);
            return RedirectToPage("/Notifications", new { sent = summary.Sent, skipped = summary.Skipped, failed = summary.Failed });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadAsync(context);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostTestAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Family is null)
        {
            return RedirectToPage("/Account/Login");
        }

        if (!context.IsAdmin)
        {
            ModelState.AddModelError("", "Seul un administrateur peut tester les notifications.");
            await LoadAsync(context);
            return Page();
        }

        var result = await _emailService.SendAsync(
            context.User.Email,
            "Vectis - Test des notifications",
            $"Bonjour,\n\nCeci est un e-mail de test pour verifier la configuration SMTP de Vectis.\n\nFamille : {context.Family.Name}\n\nVectis");

        if (!result.Sent)
        {
            ModelState.AddModelError("", result.Message);
            TestResult = result;
            await LoadAsync(context);
            return Page();
        }

        return RedirectToPage("/Notifications", new { test = "sent" });
    }

    public string StatusLabel(string status)
    {
        return status switch
        {
            "sent" => "Envoye",
            "failed" => "Echec",
            "ignored" => "Ignore",
            _ => status
        };
    }

    private async Task LoadAsync(CurrentContext context)
    {
        CanSend = context.IsAdmin;
        Overview = await _notificationService.GetOverviewAsync(context.User.Id, context.Family!.Id);
    }
}
