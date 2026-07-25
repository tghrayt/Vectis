using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

[Authorize]
public sealed class HistoryModel : PageModel
{
    private readonly CurrentUser _currentUser;
    private readonly HistoryService _historyService;

    public HistoryModel(CurrentUser currentUser, HistoryService historyService)
    {
        _currentUser = currentUser;
        _historyService = historyService;
    }

    public IReadOnlyList<PumpingSession> PumpingSessions { get; private set; } = [];
    public IReadOnlyList<Feeding> Feedings { get; private set; } = [];
    public IReadOnlyList<AuditEntry> AuditEntries { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Baby is null)
        {
            return RedirectToPage("/Account/Login");
        }

        var history = await _historyService.GetAsync(context.Baby.Id);
        PumpingSessions = history.PumpingSessions;
        Feedings = history.Feedings;
        AuditEntries = history.AuditEntries;
        return Page();
    }
}
