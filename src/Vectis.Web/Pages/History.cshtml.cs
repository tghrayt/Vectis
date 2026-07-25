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
    private readonly JsonAppStore _store;

    public HistoryModel(CurrentUser currentUser, JsonAppStore store)
    {
        _currentUser = currentUser;
        _store = store;
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

        var state = await _store.LoadAsync();
        PumpingSessions = state.PumpingSessions.Where(item => item.BabyId == context.Baby.Id).OrderByDescending(item => item.PumpedAt).ToList();
        Feedings = state.Feedings.Where(item => item.BabyId == context.Baby.Id).OrderByDescending(item => item.StartedAt).ToList();
        AuditEntries = state.AuditEntries.OrderByDescending(item => item.OccurredAt).Take(30).ToList();
        return Page();
    }
}
