using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

[Authorize]
public sealed class NotificationsModel : PageModel
{
    private readonly CurrentUser _currentUser;
    private readonly NotificationService _notificationService;

    public NotificationsModel(CurrentUser currentUser, NotificationService notificationService)
    {
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public NotificationOverview? Overview { get; private set; }
    public NotificationSendSummary? SendSummary { get; private set; }
    public bool CanSend { get; private set; }

    public async Task<IActionResult> OnGetAsync(int? sent, int? skipped, int? failed)
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
