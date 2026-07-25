using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly CurrentUser _currentUser;
    private readonly StockService _stockService;
    private readonly HistoryService _historyService;
    private readonly FamilyService _familyService;

    public IndexModel(CurrentUser currentUser, StockService stockService, HistoryService historyService, FamilyService familyService)
    {
        _currentUser = currentUser;
        _stockService = stockService;
        _historyService = historyService;
        _familyService = familyService;
    }

    public StockSummary? Summary { get; private set; }
    public string BabyName { get; private set; } = "";
    public int MemberCount { get; private set; }
    public int PendingInvitationCount { get; private set; }
    public int FeedingCountToday { get; private set; }
    public IReadOnlyList<Feeding> RecentFeedings { get; private set; } = [];
    public IReadOnlyList<PumpingSession> RecentPumpingSessions { get; private set; } = [];
    public string StockHealthLabel { get; private set; } = "Stable";
    public string StockHealthClass { get; private set; } = "ok";

    public async Task<IActionResult> OnGetAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Baby is null || context.Family is null)
        {
            return RedirectToPage("/Account/Login");
        }

        BabyName = context.Baby.FirstName;
        Summary = await _stockService.GetSummaryAsync(context.User.Id, context.Baby.Id);
        var history = await _historyService.GetAsync(context.User.Id, context.Baby.Id);
        var users = await _familyService.GetUsersAsync(context.Family.Id, context.User.Id);
        var today = DateTimeOffset.Now.Date;

        MemberCount = users.Members.Count;
        PendingInvitationCount = users.Invitations.Count(invitation => invitation.Status == "pending");
        FeedingCountToday = history.Feedings.Count(feeding => feeding.StartedAt.ToLocalTime().Date == today);
        RecentFeedings = history.Feedings.Take(5).ToList();
        RecentPumpingSessions = history.PumpingSessions.Take(5).ToList();
        SetStockHealth(Summary);
        return Page();
    }

    private void SetStockHealth(StockSummary summary)
    {
        if (summary.TotalAvailableMl <= 0)
        {
            StockHealthLabel = "Stock vide";
            StockHealthClass = "danger";
            return;
        }

        if (summary.ExpiringSoonMl > 0)
        {
            StockHealthLabel = "A utiliser vite";
            StockHealthClass = "warning";
            return;
        }

        StockHealthLabel = "Stable";
        StockHealthClass = "ok";
    }
}
